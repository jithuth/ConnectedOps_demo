using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class VehicleTelemetryStateService : IVehicleTelemetryStateService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDeviceConnectivityService _connectivityService;

    public VehicleTelemetryStateService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDeviceConnectivityService connectivityService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _connectivityService = connectivityService;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for telemetry operations.");
    }

    public async Task<VehicleTelemetryStateDto?> GetVehicleTelemetryStateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var state = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.TrackingDevice)
            .FirstOrDefaultAsync(x => x.VehicleId == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (state == null)
            return null;

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        return new VehicleTelemetryStateDto(
            state.VehicleId,
            state.Vehicle.VehicleNumber,
            state.Vehicle.DisplayName ?? state.Vehicle.VehicleNumber,
            state.TenantId,
            state.TrackingDeviceId,
            state.TrackingDevice.DeviceIdentifier,
            state.RecordedAtUtc,
            state.ReceivedAtUtc,
            state.Latitude,
            state.Longitude,
            state.AltitudeMeters,
            state.SpeedKph,
            state.HeadingDegrees,
            state.IgnitionOn,
            state.OdometerKm,
            state.EngineHours,
            state.FuelLevelPercent,
            state.BatteryVoltage,
            state.ExternalPowerVoltage,
            state.SignalStrength,
            _connectivityService.EvaluateStatus(state.TrackingDevice.LastSeenAtUtc, threshold),
            state.LastUpdatedAtUtc);
    }

    public async Task<IReadOnlyCollection<LiveVehicleTrackingDto>> GetLiveFleetTrackingAsync(
        LiveTrackingQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        var query = _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.TrackingDevice)
            .Where(x => x.TenantId == tenantId && !x.Vehicle.IsDeleted);

        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.Vehicle.BranchId == parameters.BranchId.Value);
        }

        if (parameters.VehicleStatus.HasValue)
        {
            var vStatus = (ConnectedOps.Domain.Vehicles.VehicleStatus)parameters.VehicleStatus.Value;
            query = query.Where(x => x.Vehicle.Status == vStatus);
        }

        if (parameters.IgnitionOn.HasValue)
        {
            query = query.Where(x => x.IgnitionOn == parameters.IgnitionOn.Value);
        }

        if (parameters.IsMoving.HasValue)
        {
            if (parameters.IsMoving.Value)
            {
                query = query.Where(x => x.SpeedKph != null && x.SpeedKph.Value > 0);
            }
            else
            {
                query = query.Where(x => x.SpeedKph == null || x.SpeedKph.Value == 0);
            }
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Vehicle.VehicleNumber.ToLower().Contains(search) ||
                (x.Vehicle.DisplayName != null && x.Vehicle.DisplayName.ToLower().Contains(search)) ||
                (x.Vehicle.RegistrationNumber != null && x.Vehicle.RegistrationNumber.ToLower().Contains(search)) ||
                x.TrackingDevice.DeviceIdentifier.ToLower().Contains(search));
        }

        var states = await query
            .OrderByDescending(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        // Fetch active usage sessions for active drivers
        var vehicleIds = states.Select(x => x.VehicleId).ToList();
        var activeSessions = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Include(s => s.Driver)
            .Where(s => vehicleIds.Contains(s.VehicleId) && s.Status == UsageSessionStatus.Open && s.TenantId == tenantId)
            .ToDictionaryAsync(s => s.VehicleId, cancellationToken);

        // Fetch branch names
        var branchIds = states.Where(x => x.Vehicle.BranchId.HasValue).Select(x => x.Vehicle.BranchId!.Value).Distinct().ToList();
        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(b => branchIds.Contains(b.Id) && b.TenantId == tenantId)
            .ToDictionaryAsync(b => b.Id, b => b.Name, cancellationToken);

        var list = new List<LiveVehicleTrackingDto>(states.Count);
        foreach (var s in states)
        {
            var connStatus = _connectivityService.EvaluateStatus(s.TrackingDevice.LastSeenAtUtc, threshold);

            if (parameters.ConnectivityStatus.HasValue && connStatus != parameters.ConnectivityStatus.Value)
            {
                continue;
            }

            activeSessions.TryGetValue(s.VehicleId, out var session);
            string? branchName = null;
            if (s.Vehicle.BranchId.HasValue)
            {
                branches.TryGetValue(s.Vehicle.BranchId.Value, out branchName);
            }

            list.Add(new LiveVehicleTrackingDto(
                s.VehicleId,
                s.Vehicle.VehicleNumber,
                s.Vehicle.RegistrationNumber ?? "",
                s.Vehicle.DisplayName ?? s.Vehicle.VehicleNumber,
                s.TrackingDeviceId,
                s.TrackingDevice.DeviceIdentifier,
                s.TrackingDevice.Name,
                s.Latitude,
                s.Longitude,
                s.SpeedKph,
                s.HeadingDegrees,
                s.IgnitionOn,
                s.RecordedAtUtc,
                connStatus,
                session?.DriverId,
                session != null ? $"{session.Driver.FirstName} {session.Driver.LastName}" : null,
                s.Vehicle.BranchId,
                branchName,
                s.TrackingDevice.BatteryLevelPercent,
                s.SignalStrength,
                s.OdometerKm));
        }

        return list;
    }
}
