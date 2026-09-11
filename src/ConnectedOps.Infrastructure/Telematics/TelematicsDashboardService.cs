using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelematicsDashboardService : ITelematicsDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDeviceConnectivityService _connectivityService;

    public TelematicsDashboardService(
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
               throw new InvalidOperationException("Active tenant context is required for telematics dashboard.");
    }

    public async Task<TelematicsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int threshold = settings?.OfflineThresholdMinutes ?? 5;

        // 1. All tenant devices
        var devices = await _dbContext.TrackingDevices
            .AsNoTracking()
            .Include(x => x.Provider)
            .Include(x => x.DeviceType)
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        int totalDevices = devices.Count;
        int onlineCount = 0;
        int offlineCount = 0;
        int faultedCount = devices.Count(d => d.Status == TrackingDeviceStatus.Faulted);

        var byProvider = new Dictionary<string, int>();
        var byModel = new Dictionary<string, int>();
        var byHealth = new Dictionary<string, int>
        {
            [DeviceHealthState.Healthy.ToString()] = 0,
            [DeviceHealthState.Warning.ToString()] = 0,
            [DeviceHealthState.Offline.ToString()] = 0,
            [DeviceHealthState.Faulted.ToString()] = 0,
            [DeviceHealthState.Unknown.ToString()] = 0
        };

        var deviceListItems = new List<TrackingDeviceListItemDto>(devices.Count);

        var deviceIds = devices.Select(x => x.Id).ToList();
        var assignments = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Include(a => a.Vehicle)
            .Where(a => deviceIds.Contains(a.TrackingDeviceId) && a.IsActive && a.TenantId == tenantId)
            .ToDictionaryAsync(a => a.TrackingDeviceId, cancellationToken);

        foreach (var d in devices)
        {
            var conn = _connectivityService.EvaluateStatus(d.LastSeenAtUtc, threshold);
            if (conn == DeviceConnectivityStatus.Online) onlineCount++;
            else if (conn == DeviceConnectivityStatus.Offline) offlineCount++;

            // Provider stats
            var pCode = d.Provider.Name;
            byProvider[pCode] = byProvider.GetValueOrDefault(pCode, 0) + 1;

            // Model stats
            var model = string.IsNullOrWhiteSpace(d.Model) ? "Unknown" : d.Model;
            byModel[model] = byModel.GetValueOrDefault(model, 0) + 1;

            // Health state
            var health = DeviceHealthState.Healthy;
            if (d.Status == TrackingDeviceStatus.Faulted) health = DeviceHealthState.Faulted;
            else if (!d.LastSeenAtUtc.HasValue) health = DeviceHealthState.Unknown;
            else if (conn == DeviceConnectivityStatus.Offline) health = DeviceHealthState.Offline;
            else if ((d.BatteryLevelPercent.HasValue && d.BatteryLevelPercent.Value < 20) ||
                     (d.BatteryVoltage.HasValue && d.BatteryVoltage.Value < 3.5m) ||
                     (d.SignalStrength.HasValue && d.SignalStrength.Value <= 1))
            {
                health = DeviceHealthState.Warning;
            }

            byHealth[health.ToString()]++;

            assignments.TryGetValue(d.Id, out var assignment);
            deviceListItems.Add(new TrackingDeviceListItemDto(
                d.Id,
                d.DeviceIdentifier,
                d.IMEI,
                d.Name,
                d.Provider.Code,
                d.DeviceType.Name,
                d.Status,
                conn,
                d.LastSeenAtUtc,
                assignment?.VehicleId,
                assignment?.Vehicle.DisplayName ?? assignment?.Vehicle.VehicleNumber,
                d.BatteryLevelPercent,
                d.SignalStrength,
                d.IsActive));
        }

        // 2. Vehicle tracking counts
        int totalVehicles = await _dbContext.Vehicles
            .CountAsync(v => v.TenantId == tenantId && !v.IsDeleted, cancellationToken);

        int trackedVehicles = await _dbContext.TrackingDeviceVehicleAssignments
            .Where(a => a.TenantId == tenantId && a.IsActive)
            .Select(a => a.VehicleId)
            .Distinct()
            .CountAsync(cancellationToken);

        int untrackedVehicles = Math.Max(0, totalVehicles - trackedVehicles);

        // 3. Vehicle telemetry state metrics
        var vehicleStates = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        int vehiclesMoving = vehicleStates.Count(x => x.SpeedKph.HasValue && x.SpeedKph.Value > 0);
        int vehiclesStopped = vehicleStates.Count(x => !x.SpeedKph.HasValue || x.SpeedKph.Value == 0);
        int ignitionOn = vehicleStates.Count(x => x.IgnitionOn == true);
        int ignitionOff = vehicleStates.Count(x => x.IgnitionOn != true);

        // 4. Telemetry received today (UTC)
        var todayUtc = DateTime.UtcNow.Date;
        int telemetryToday = await _dbContext.TelemetryRecords
            .CountAsync(x => x.TenantId == tenantId && x.ReceivedAtUtc >= todayUtc, cancellationToken);

        var recentlySeen = deviceListItems
            .Where(x => x.LastSeenAtUtc.HasValue)
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(5)
            .ToList();

        var recentlyOffline = deviceListItems
            .Where(x => x.ConnectivityStatus == DeviceConnectivityStatus.Offline && x.LastSeenAtUtc.HasValue)
            .OrderByDescending(x => x.LastSeenAtUtc)
            .Take(5)
            .ToList();

        return new TelematicsDashboardDto(
            TotalDevices: totalDevices,
            OnlineDevices: onlineCount,
            OfflineDevices: offlineCount,
            FaultedDevices: faultedCount,
            TrackedVehicles: trackedVehicles,
            UntrackedVehicles: untrackedVehicles,
            VehiclesMoving: vehiclesMoving,
            VehiclesStopped: vehiclesStopped,
            VehiclesIgnitionOn: ignitionOn,
            VehiclesIgnitionOff: ignitionOff,
            TelemetryReceivedToday: telemetryToday,
            DevicesByProvider: byProvider,
            DevicesByModel: byModel,
            DevicesByHealth: byHealth,
            RecentlyOfflineDevices: recentlyOffline,
            RecentlySeenDevices: recentlySeen);
    }
}
