using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Ev;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Ev;

public sealed class EvService : IEvService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<EvService> _logger;

    public EvService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<EvService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for EV fleet operations.");
    }

    public async Task<EvFleetDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var batteryStates = await _dbContext.VehicleBatteryStates
            .AsNoTracking()
            .Include(b => b.Vehicle)
            .Where(b => b.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync(cancellationToken);

        var activeSessions = await _dbContext.ChargingSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.ChargingStation)
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .OrderByDescending(s => s.StartedAtUtc)
            .ToListAsync(cancellationToken);

        var completedSessions = await _dbContext.ChargingSessions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && !s.IsActive)
            .ToListAsync(cancellationToken);

        var totalEvCount = batteryStates.Count;
        var activeChargingCount = batteryStates.Count(b => b.ChargingStatus == EvChargingStatus.ChargingAc || b.ChargingStatus == EvChargingStatus.ChargingDcFast);
        var avgSoc = totalEvCount > 0 ? Math.Round(batteryStates.Average(b => b.StateOfChargePercent), 1) : 0m;
        var avgSoh = totalEvCount > 0 ? Math.Round(batteryStates.Average(b => b.StateOfHealthPercent), 1) : 0m;

        var totalEnergyDelivered = completedSessions.Sum(s => s.EnergyDeliveredKwh);
        var totalCo2Saved = completedSessions.Sum(s => s.Co2SavedKg);
        var totalCost = completedSessions.Sum(s => s.TotalCost);

        var vehicleDtos = batteryStates.Select(MapBatteryStateToDto).ToList();
        var stationDtos = stations.Select(MapStationToDto).ToList();
        var sessionDtos = activeSessions.Select(MapSessionToDto).ToList();

        return new EvFleetDashboardDto(
            TotalEvCount: totalEvCount,
            ActiveChargingCount: activeChargingCount,
            AverageSocPercent: avgSoc,
            AverageSohPercent: avgSoh,
            TotalEnergyDeliveredKwh: Math.Round(totalEnergyDelivered, 2),
            TotalCo2SavedKg: Math.Round(totalCo2Saved, 2),
            TotalChargingCost: Math.Round(totalCost, 2),
            Vehicles: vehicleDtos,
            Stations: stationDtos,
            ActiveSessions: sessionDtos);
    }

    public async Task<PagedResult<VehicleBatteryStateDto>> GetVehicleBatteriesPagedAsync(
        EvFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.VehicleBatteryStates
            .AsNoTracking()
            .Include(b => b.Vehicle)
            .Where(b => b.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(b => (b.Vehicle.VIN != null && b.Vehicle.VIN.Contains(search)) ||
                                     (b.Vehicle.RegistrationNumber != null && b.Vehicle.RegistrationNumber.Contains(search)) ||
                                     b.Vehicle.DisplayName.Contains(search));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.ChargingStatus == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(b => b.LastTelemetryAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapBatteryStateToDto).ToList();

        return new PagedResult<VehicleBatteryStateDto>(
            Items: dtos,
            TotalCount: totalCount,
            PageNumber: page,
            PageSize: pageSize);
    }

    public async Task<IReadOnlyList<ChargingStationDto>> GetStationsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var stations = await _dbContext.ChargingStations
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return stations.Select(MapStationToDto).ToList();
    }

    public async Task<ChargingStationDto> CreateStationAsync(
        CreateChargingStationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var existing = await _dbContext.ChargingStations
            .AnyAsync(s => s.TenantId == tenantId && s.Code == request.Code.Trim().ToUpperInvariant(), cancellationToken);

        if (existing)
            throw new InvalidOperationException($"Charging station with code '{request.Code}' already exists.");

        var station = new ChargingStation(
            tenantId: tenantId,
            code: request.Code,
            name: request.Name,
            stationType: request.StationType,
            connectorType: request.ConnectorType,
            maxPowerKw: request.MaxPowerKw,
            totalPlugs: request.TotalPlugs,
            address: request.Address,
            latitude: request.Latitude,
            longitude: request.Longitude,
            offPeakRatePerKwh: request.OffPeakRatePerKwh,
            peakRatePerKwh: request.PeakRatePerKwh,
            currency: request.Currency);

        _dbContext.ChargingStations.Add(station);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created charging station {StationCode} for tenant {TenantId}", station.Code, tenantId);
        return MapStationToDto(station);
    }

    public async Task<VehicleBatteryStateDto> UpdateTelemetryAsync(
        UpdateVehicleBatteryTelemetryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Vehicle {request.VehicleId} not found.");

        var batteryState = await _dbContext.VehicleBatteryStates
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.VehicleId == request.VehicleId && b.TenantId == tenantId, cancellationToken);

        if (batteryState == null)
        {
            batteryState = new VehicleBatteryState(
                tenantId: tenantId,
                vehicleId: request.VehicleId,
                batteryCapacityKwh: 75.0m,
                stateOfChargePercent: request.StateOfChargePercent,
                remainingRangeKm: request.RemainingRangeKm,
                batteryPackTempCelsius: request.BatteryPackTempCelsius);
            batteryState.Vehicle = vehicle;
            _dbContext.VehicleBatteryStates.Add(batteryState);
        }

        batteryState.UpdateTelemetry(
            soc: request.StateOfChargePercent,
            remainingRange: request.RemainingRangeKm,
            packTemp: request.BatteryPackTempCelsius,
            status: request.ChargingStatus,
            chargingPowerKw: request.ActiveChargingPowerKw);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapBatteryStateToDto(batteryState);
    }

    public async Task<VehicleBatteryStateDto> ConfigureSmartChargingAsync(
        ConfigureSmartChargingRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var batteryState = await _dbContext.VehicleBatteryStates
            .Include(b => b.Vehicle)
            .FirstOrDefaultAsync(b => b.VehicleId == request.VehicleId && b.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException($"Battery state for vehicle {request.VehicleId} not found.");

        batteryState.ConfigureSmartCharging(request.TargetSocLimitPercent, request.IsOffPeakOnlyCharging);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapBatteryStateToDto(batteryState);
    }

    public async Task<ChargingSessionDto> StartSessionAsync(
        StartChargingSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var station = await _dbContext.ChargingStations
            .FirstOrDefaultAsync(s => s.Id == request.ChargingStationId && s.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Charging station not found.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Vehicle not found.");

        var activeSessionsForStation = await _dbContext.ChargingSessions
            .CountAsync(s => s.ChargingStationId == station.Id && s.IsActive, cancellationToken);

        if (activeSessionsForStation >= station.TotalPlugs)
            throw new InvalidOperationException("All plugs at this charging station are currently occupied.");

        var session = new ChargingSession(
            tenantId: tenantId,
            vehicleId: request.VehicleId,
            chargingStationId: request.ChargingStationId,
            startSocPercent: request.StartSocPercent,
            isScheduled: request.IsScheduled,
            scheduledStartUtc: request.ScheduledStartUtc,
            currency: station.Currency);

        session.Vehicle = vehicle;
        session.ChargingStation = station;

        _dbContext.ChargingSessions.Add(session);

        // Update station available plugs
        station.UpdateOccupancy(activeSessionsForStation + 1);

        // Update vehicle battery charging status
        var batteryState = await _dbContext.VehicleBatteryStates
            .FirstOrDefaultAsync(b => b.VehicleId == request.VehicleId && b.TenantId == tenantId, cancellationToken);
        if (batteryState != null)
        {
            var isDc = station.MaxPowerKw >= 50m;
            batteryState.UpdateTelemetry(
                soc: request.StartSocPercent,
                remainingRange: batteryState.RemainingRangeKm,
                packTemp: batteryState.BatteryPackTempCelsius,
                status: isDc ? EvChargingStatus.ChargingDcFast : EvChargingStatus.ChargingAc,
                chargingPowerKw: station.MaxPowerKw);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Started charging session for vehicle {VehicleId} at station {StationId}", request.VehicleId, request.ChargingStationId);

        return MapSessionToDto(session);
    }

    public async Task<ChargingSessionDto> CompleteSessionAsync(
        CompleteChargingSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var session = await _dbContext.ChargingSessions
            .Include(s => s.Vehicle)
            .Include(s => s.ChargingStation)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Charging session not found.");

        if (!session.IsActive)
            throw new InvalidOperationException("This charging session has already been completed.");

        session.CompleteSession(request.EndSocPercent, request.EnergyDeliveredKwh, request.TotalCost);

        // Update station plug count
        var activeSessionsCount = await _dbContext.ChargingSessions
            .CountAsync(s => s.ChargingStationId == session.ChargingStationId && s.IsActive && s.Id != session.Id, cancellationToken);
        session.ChargingStation.UpdateOccupancy(activeSessionsCount);

        // Update vehicle battery state
        var battery = await _dbContext.VehicleBatteryStates
            .FirstOrDefaultAsync(b => b.VehicleId == session.VehicleId && b.TenantId == tenantId, cancellationToken);
        if (battery != null)
        {
            var addedRange = request.EnergyDeliveredKwh * 5.5m; // approx 5.5 km per kWh
            battery.UpdateTelemetry(
                soc: request.EndSocPercent,
                remainingRange: Math.Round(battery.RemainingRangeKm + addedRange, 1),
                packTemp: 28m,
                status: request.EndSocPercent >= 98m ? EvChargingStatus.FullyCharged : EvChargingStatus.ConnectedIdle,
                chargingPowerKw: 0m);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Completed charging session {SessionId}. Delivered: {Kwh} kWh, Cost: {Cost}", session.Id, request.EnergyDeliveredKwh, request.TotalCost);

        return MapSessionToDto(session);
    }

    public async Task<PagedResult<ChargingSessionDto>> GetSessionsPagedAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var p = Math.Max(1, page);
        var ps = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.ChargingSessions
            .AsNoTracking()
            .Include(s => s.Vehicle)
            .Include(s => s.ChargingStation)
            .Where(s => s.TenantId == tenantId);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(s => s.StartedAtUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapSessionToDto).ToList();

        return new PagedResult<ChargingSessionDto>(
            Items: dtos,
            TotalCount: total,
            PageNumber: p,
            PageSize: ps);
    }

    private static VehicleBatteryStateDto MapBatteryStateToDto(VehicleBatteryState b)
    {
        return new VehicleBatteryStateDto(
            Id: b.Id,
            TenantId: b.TenantId,
            VehicleId: b.VehicleId,
            VehicleVin: b.Vehicle?.VIN ?? "N/A",
            VehicleName: b.Vehicle != null ? $"{b.Vehicle.DisplayName} ({b.Vehicle.RegistrationNumber ?? b.Vehicle.VehicleNumber})" : "Unknown Vehicle",
            BatteryCapacityKwh: b.BatteryCapacityKwh,
            StateOfChargePercent: b.StateOfChargePercent,
            StateOfHealthPercent: b.StateOfHealthPercent,
            RemainingRangeKm: b.RemainingRangeKm,
            BatteryPackTempCelsius: b.BatteryPackTempCelsius,
            ChargingStatus: b.ChargingStatus,
            ActiveChargingPowerKw: b.ActiveChargingPowerKw,
            CycleCount: b.CycleCount,
            TargetSocLimitPercent: b.TargetSocLimitPercent,
            IsOffPeakOnlyCharging: b.IsOffPeakOnlyCharging,
            HealthCondition: b.HealthCondition,
            LastTelemetryAtUtc: b.LastTelemetryAtUtc);
    }

    private static ChargingStationDto MapStationToDto(ChargingStation s)
    {
        return new ChargingStationDto(
            Id: s.Id,
            TenantId: s.TenantId,
            Code: s.Code,
            Name: s.Name,
            StationType: s.StationType,
            ConnectorType: s.ConnectorType,
            MaxPowerKw: s.MaxPowerKw,
            TotalPlugs: s.TotalPlugs,
            AvailablePlugs: s.AvailablePlugs,
            Address: s.Address,
            Latitude: s.Latitude,
            Longitude: s.Longitude,
            OffPeakRatePerKwh: s.OffPeakRatePerKwh,
            PeakRatePerKwh: s.PeakRatePerKwh,
            Currency: s.Currency,
            IsActive: s.IsActive);
    }

    private static ChargingSessionDto MapSessionToDto(ChargingSession s)
    {
        return new ChargingSessionDto(
            Id: s.Id,
            TenantId: s.TenantId,
            VehicleId: s.VehicleId,
            VehicleVin: s.Vehicle?.VIN ?? "N/A",
            ChargingStationId: s.ChargingStationId,
            StationName: s.ChargingStation?.Name ?? "Charging Station",
            StartedAtUtc: s.StartedAtUtc,
            CompletedAtUtc: s.CompletedAtUtc,
            StartSocPercent: s.StartSocPercent,
            EndSocPercent: s.EndSocPercent,
            EnergyDeliveredKwh: s.EnergyDeliveredKwh,
            TotalCost: s.TotalCost,
            Co2SavedKg: s.Co2SavedKg,
            Currency: s.Currency,
            IsActive: s.IsActive,
            IsScheduled: s.IsScheduled,
            ScheduledStartUtc: s.ScheduledStartUtc);
    }
}
