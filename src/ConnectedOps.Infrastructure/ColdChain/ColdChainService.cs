using ConnectedOps.Application.ColdChain;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.ColdChain;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.ColdChain;

public sealed class ColdChainService : IColdChainService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ColdChainService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        if (!_currentUserContext.TenantId.HasValue || _currentUserContext.TenantId.Value == Guid.Empty)
            throw new InvalidOperationException("Tenant context is required.");
        return _currentUserContext.TenantId.Value;
    }

    public async Task<ColdChainDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sensors = await _dbContext.CargoSensorDevices
            .Include(s => s.Vehicle)
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var excursions = await _dbContext.ColdChainExcursions
            .Include(e => e.CargoSensorDevice)
            .Include(e => e.Vehicle)
            .Where(e => e.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var activeSensors = sensors.Where(s => s.IsActive).ToList();
        var compartmentsInTolerance = activeSensors.Count(s =>
            s.CurrentTemperatureCelsius.HasValue &&
            s.CurrentTemperatureCelsius.Value >= s.MinTargetTemperatureCelsius &&
            s.CurrentTemperatureCelsius.Value <= s.MaxTargetTemperatureCelsius);

        var activeExcursions = excursions.Where(e => e.Status == ExcursionStatus.Active || e.Status == ExcursionStatus.Acknowledged).ToList();
        var criticalExcursions = activeExcursions.Count(e => e.Severity == ExcursionSeverity.Critical || e.Severity == ExcursionSeverity.HaccpBreach);

        return new ColdChainDashboardDto(
            TotalActiveSensors: activeSensors.Count,
            CompartmentsInToleranceCount: compartmentsInTolerance,
            ActiveExcursionsCount: activeExcursions.Count,
            CriticalExcursionsCount: criticalExcursions,
            ActiveSensors: activeSensors.OrderBy(s => s.SensorTagNumber).Select(MapToSensorDto).ToList(),
            RecentExcursions: excursions.OrderByDescending(e => e.StartedAtUtc).Take(10).Select(MapToExcursionDto).ToList());
    }

    public async Task<PagedResult<CargoSensorDeviceDto>> GetSensorsPagedAsync(SensorFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.CargoSensorDevices
            .Include(s => s.Vehicle)
            .Where(s => s.TenantId == tenantId);

        if (request.VehicleId.HasValue)
            query = query.Where(s => s.VehicleId == request.VehicleId.Value);

        if (request.IsActive.HasValue)
            query = query.Where(s => s.IsActive == request.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(s =>
                s.SensorTagNumber.ToLower().Contains(term) ||
                s.CompartmentName.ToLower().Contains(term) ||
                (s.Vehicle.RegistrationNumber != null && s.Vehicle.RegistrationNumber.ToLower().Contains(term)) ||
                s.Vehicle.VehicleNumber.ToLower().Contains(term) ||
                (s.MacAddress != null && s.MacAddress.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(s => s.SensorTagNumber)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CargoSensorDeviceDto>(
            items.Select(MapToSensorDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<PagedResult<ColdChainExcursionDto>> GetExcursionsPagedAsync(ExcursionFilterRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.ColdChainExcursions
            .Include(e => e.CargoSensorDevice)
            .Include(e => e.Vehicle)
            .Where(e => e.TenantId == tenantId);

        if (request.Status.HasValue)
            query = query.Where(e => e.Status == request.Status.Value);

        if (request.Severity.HasValue)
            query = query.Where(e => e.Severity == request.Severity.Value);

        if (request.VehicleId.HasValue)
            query = query.Where(e => e.VehicleId == request.VehicleId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.StartedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ColdChainExcursionDto>(
            items.Select(MapToExcursionDto).ToList(),
            totalCount,
            request.PageNumber,
            request.PageSize);
    }

    public async Task<CargoSensorDeviceDto> RegisterSensorAsync(RegisterCargoSensorRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == request.VehicleId && v.TenantId == tenantId, cancellationToken);
        if (vehicle == null)
            throw new KeyNotFoundException($"Vehicle with ID {request.VehicleId} was not found.");

        var sensor = new CargoSensorDevice(
            tenantId,
            request.SensorTagNumber,
            request.CompartmentName,
            request.VehicleId,
            request.MinTargetTemperatureCelsius,
            request.MaxTargetTemperatureCelsius,
            request.BatteryLevelPercent,
            request.MacAddress);

        sensor.Vehicle = vehicle;
        await _dbContext.CargoSensorDevices.AddAsync(sensor, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToSensorDto(sensor);
    }

    public async Task<CargoTelemetryReadingDto> RecordTelemetryAsync(RecordCargoTelemetryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var sensor = await _dbContext.CargoSensorDevices
            .Include(s => s.Vehicle)
            .FirstOrDefaultAsync(s => s.Id == request.CargoSensorDeviceId && s.TenantId == tenantId, cancellationToken);

        if (sensor == null)
            throw new KeyNotFoundException($"Cargo sensor with ID {request.CargoSensorDeviceId} was not found.");

        sensor.UpdateLatestTelemetry(request.TemperatureCelsius, request.HumidityPercent, request.DoorOpen, null);

        var reading = new CargoTelemetryReading(
            tenantId,
            sensor.Id,
            request.RecordedAtUtc,
            request.TemperatureCelsius,
            request.HumidityPercent,
            request.DoorOpen,
            request.ReeferMode,
            request.SetpointTemperatureCelsius,
            request.Latitude,
            request.Longitude);

        reading.CargoSensorDevice = sensor;
        await _dbContext.CargoTelemetryReadings.AddAsync(reading, cancellationToken);

        // Check if temperature excursion occurred
        if (request.TemperatureCelsius < sensor.MinTargetTemperatureCelsius ||
            request.TemperatureCelsius > sensor.MaxTargetTemperatureCelsius)
        {
            var diff = request.TemperatureCelsius < sensor.MinTargetTemperatureCelsius
                ? sensor.MinTargetTemperatureCelsius - request.TemperatureCelsius
                : request.TemperatureCelsius - sensor.MaxTargetTemperatureCelsius;

            var severity = diff >= 6.0 ? ExcursionSeverity.HaccpBreach
                         : diff >= 3.0 ? ExcursionSeverity.Critical
                         : ExcursionSeverity.Warning;

            // Check if there is an active excursion already open for this sensor
            var activeExcursion = await _dbContext.ColdChainExcursions
                .FirstOrDefaultAsync(e => e.CargoSensorDeviceId == sensor.Id
                    && (e.Status == ExcursionStatus.Active || e.Status == ExcursionStatus.Acknowledged)
                    && e.TenantId == tenantId, cancellationToken);

            if (activeExcursion == null)
            {
                var excursion = new ColdChainExcursion(
                    tenantId,
                    sensor.Id,
                    sensor.VehicleId,
                    sensor.CompartmentName,
                    request.TemperatureCelsius,
                    sensor.MinTargetTemperatureCelsius,
                    sensor.MaxTargetTemperatureCelsius,
                    severity,
                    request.RecordedAtUtc,
                    locationName: "Transit Route Telemetry");

                await _dbContext.ColdChainExcursions.AddAsync(excursion, cancellationToken);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CargoTelemetryReadingDto(
            Id: reading.Id,
            CargoSensorDeviceId: reading.CargoSensorDeviceId,
            SensorTagNumber: sensor.SensorTagNumber,
            RecordedAtUtc: reading.RecordedAtUtc,
            TemperatureCelsius: reading.TemperatureCelsius,
            HumidityPercent: reading.HumidityPercent,
            DoorOpen: reading.DoorOpen,
            ReeferMode: reading.ReeferMode,
            ReeferModeName: reading.ReeferMode.ToString(),
            SetpointTemperatureCelsius: reading.SetpointTemperatureCelsius,
            Latitude: reading.Latitude,
            Longitude: reading.Longitude);
    }

    public async Task<ColdChainExcursionDto> ResolveExcursionAsync(Guid excursionId, ResolveExcursionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var excursion = await _dbContext.ColdChainExcursions
            .Include(e => e.CargoSensorDevice)
            .Include(e => e.Vehicle)
            .FirstOrDefaultAsync(e => e.Id == excursionId && e.TenantId == tenantId, cancellationToken);

        if (excursion == null)
            throw new KeyNotFoundException($"Cold chain excursion with ID {excursionId} was not found.");

        excursion.Resolve(request.ActionTaken, _currentUserContext.UserId ?? Guid.Empty);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToExcursionDto(excursion);
    }

    private static CargoSensorDeviceDto MapToSensorDto(CargoSensorDevice s) =>
        new(
            Id: s.Id,
            SensorTagNumber: s.SensorTagNumber,
            CompartmentName: s.CompartmentName,
            VehicleId: s.VehicleId,
            VehiclePlateNumber: s.Vehicle != null ? (s.Vehicle.RegistrationNumber ?? s.Vehicle.VehicleNumber) : "N/A",
            MinTargetTemperatureCelsius: s.MinTargetTemperatureCelsius,
            MaxTargetTemperatureCelsius: s.MaxTargetTemperatureCelsius,
            BatteryLevelPercent: s.BatteryLevelPercent,
            MacAddress: s.MacAddress,
            IsActive: s.IsActive,
            CurrentTemperatureCelsius: s.CurrentTemperatureCelsius,
            CurrentHumidityPercent: s.CurrentHumidityPercent,
            CurrentDoorOpen: s.CurrentDoorOpen,
            LastReadingAtUtc: s.LastReadingAtUtc);

    private static ColdChainExcursionDto MapToExcursionDto(ColdChainExcursion e) =>
        new(
            Id: e.Id,
            CargoSensorDeviceId: e.CargoSensorDeviceId,
            SensorTagNumber: e.CargoSensorDevice?.SensorTagNumber ?? "N/A",
            VehicleId: e.VehicleId,
            VehiclePlateNumber: e.Vehicle != null ? (e.Vehicle.RegistrationNumber ?? e.Vehicle.VehicleNumber) : "N/A",
            CompartmentName: e.CargoSensorDevice?.CompartmentName ?? e.CompartmentName,
            BreachTemperatureCelsius: e.BreachTemperatureCelsius,
            AllowableMinCelsius: e.AllowableMinCelsius,
            AllowableMaxCelsius: e.AllowableMaxCelsius,
            Severity: e.Severity,
            SeverityName: e.Severity.ToString(),
            Status: e.Status,
            StatusName: e.Status.ToString(),
            StartedAtUtc: e.StartedAtUtc,
            ResolvedAtUtc: e.ResolvedAtUtc,
            DurationMinutes: e.DurationMinutes,
            LocationName: e.LocationName,
            ActionTaken: e.ActionTaken);
}
