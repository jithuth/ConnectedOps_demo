using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class DeviceHealthService : IDeviceHealthService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDeviceConnectivityService _connectivityService;

    public DeviceHealthService(
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
               throw new InvalidOperationException("Active tenant context is required for device health.");
    }

    public async Task<DeviceHealthDto?> GetDeviceHealthByIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var device = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .FirstOrDefaultAsync(x => x.Id == deviceId && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (device == null)
            return null;

        var activeAssignment = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.TrackingDeviceId == device.Id && a.IsActive, cancellationToken);

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        return MapToHealthDto(device, activeAssignment, threshold);
    }

    public async Task<IReadOnlyCollection<DeviceHealthDto>> GetDeviceHealthPagedAsync(
        DeviceHealthQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        var query = _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(x =>
                x.DeviceIdentifier.ToLower().Contains(s) ||
                (x.IMEI != null && x.IMEI.ToLower().Contains(s)) ||
                (x.Name != null && x.Name.ToLower().Contains(s)));
        }

        if (parameters.ProviderId.HasValue)
        {
            query = query.Where(x => x.ProviderId == parameters.ProviderId.Value);
        }

        if (parameters.HasVehicle.HasValue)
        {
            var assignedIds = _dbContext.TrackingDeviceVehicleAssignments
                .Where(a => a.TenantId == tenantId && a.IsActive)
                .Select(a => a.TrackingDeviceId);

            if (parameters.HasVehicle.Value)
            {
                query = query.Where(x => assignedIds.Contains(x.Id));
            }
            else
            {
                query = query.Where(x => !assignedIds.Contains(x.Id));
            }
        }

        var devices = await query
            .OrderByDescending(x => x.LastSeenAtUtc)
            .ToListAsync(cancellationToken);

        var deviceIds = devices.Select(x => x.Id).ToList();
        var assignments = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Where(a => deviceIds.Contains(a.TrackingDeviceId) && a.IsActive && a.TenantId == tenantId)
            .ToDictionaryAsync(a => a.TrackingDeviceId, cancellationToken);

        var list = new List<DeviceHealthDto>(devices.Count);
        foreach (var d in devices)
        {
            assignments.TryGetValue(d.Id, out var assignment);
            var dto = MapToHealthDto(d, assignment, threshold);

            if (parameters.HealthState.HasValue && dto.HealthState != parameters.HealthState.Value)
            {
                continue;
            }

            if (parameters.ConnectivityStatus.HasValue && dto.ConnectivityStatus != parameters.ConnectivityStatus.Value)
            {
                continue;
            }

            list.Add(dto);
        }

        int pageSize = Math.Clamp(parameters.PageSize, 1, 100);
        int page = Math.Max(1, parameters.Page);

        return list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }

    public async Task<int> GetDeviceHealthCountAsync(
        DeviceHealthQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Calculate total count respecting health/connectivity filters
        var p = parameters with { Page = 1, PageSize = int.MaxValue };
        var all = await GetDeviceHealthPagedAsync(p, cancellationToken);
        return all.Count;
    }

    private DeviceHealthDto MapToHealthDto(
        TrackingDevice d,
        TrackingDeviceVehicleAssignment? assignment,
        int threshold)
    {
        var connectivity = _connectivityService.EvaluateStatus(d.LastSeenAtUtc, threshold);

        var health = DeviceHealthState.Healthy;
        if (d.Status == TrackingDeviceStatus.Faulted)
        {
            health = DeviceHealthState.Faulted;
        }
        else if (!d.LastSeenAtUtc.HasValue)
        {
            health = DeviceHealthState.Unknown;
        }
        else if (connectivity == DeviceConnectivityStatus.Offline)
        {
            health = DeviceHealthState.Offline;
        }
        else if ((d.BatteryLevelPercent.HasValue && d.BatteryLevelPercent.Value < 20) ||
                 (d.BatteryVoltage.HasValue && d.BatteryVoltage.Value < 3.5m) ||
                 (d.SignalStrength.HasValue && d.SignalStrength.Value <= 1))
        {
            health = DeviceHealthState.Warning;
        }

        return new DeviceHealthDto(
            d.Id,
            d.DeviceIdentifier,
            d.IMEI,
            d.Name,
            d.Provider.Code,
            d.DeviceType.Name,
            assignment?.VehicleId,
            assignment?.Vehicle.DisplayName ?? assignment?.Vehicle.VehicleNumber,
            assignment?.Vehicle.RegistrationNumber,
            d.LastSeenAtUtc,
            d.LastTelemetryAtUtc,
            connectivity,
            health,
            d.BatteryLevelPercent,
            d.ExternalPowerVoltage,
            d.BatteryVoltage,
            d.SignalStrength,
            d.FirmwareVersion);
    }
}
