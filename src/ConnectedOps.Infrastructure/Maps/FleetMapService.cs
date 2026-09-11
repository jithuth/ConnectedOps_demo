using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maps;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Maps;

public sealed class FleetMapService : IFleetMapService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IDeviceConnectivityService _connectivityService;
    private readonly MapSettings _mapSettings;
    private readonly DemoFleetSettings _demoFleetSettings;

    public FleetMapService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IDeviceConnectivityService connectivityService,
        IOptions<MapSettings> mapSettings,
        IOptions<DemoFleetSettings> demoFleetSettings)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _connectivityService = connectivityService;
        _mapSettings = mapSettings.Value;
        _demoFleetSettings = demoFleetSettings.Value;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for map operations.");
    }

    public async Task<IReadOnlyCollection<FleetMapVehicleDto>> GetFleetMapVehiclesAsync(
        FleetMapQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        // 1. Get tenant offline threshold
        var telematicsSettings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        int offlineThreshold = telematicsSettings?.OfflineThresholdMinutes ?? 5;

        // 2. Query Vehicles
        var vehicleQuery = _dbContext.Vehicles
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            vehicleQuery = vehicleQuery.Where(x =>
                x.VehicleNumber.ToLower().Contains(search) ||
                x.DisplayName.ToLower().Contains(search) ||
                (x.RegistrationNumber != null && x.RegistrationNumber.ToLower().Contains(search)));
        }

        if (parameters.BranchId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(x => x.BranchId == parameters.BranchId.Value);
        }

        if (parameters.CategoryId.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(x => x.VehicleCategoryId == parameters.CategoryId.Value);
        }

        if (parameters.Status.HasValue)
        {
            vehicleQuery = vehicleQuery.Where(x => x.Status == parameters.Status.Value);
        }

        var vehicles = await vehicleQuery
            .Include(v => v.VehicleCategory)
            .Include(v => v.VehicleMake)
            .Include(v => v.VehicleModel)
            .Include(v => v.Branch)
            .ToListAsync(cancellationToken);

        if (vehicles.Count == 0)
            return [];

        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        // 3. Load latest Telemetry states
        var telemetryStates = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId))
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);

        // 4. Load active driver assignments
        var activeDriverAssignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId) && x.IsActive && x.TenantId == tenantId)
            .Include(x => x.Driver)
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);

        // 5. Load active device assignments
        var activeDeviceAssignments = await _dbContext.TrackingDeviceVehicleAssignments
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId) && x.IsActive && x.TenantId == tenantId)
            .Include(x => x.TrackingDevice)
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);

        // 6. Load geofence membership counts
        var geofenceCounts = await _dbContext.VehicleGeofenceStates
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId) && x.TenantId == tenantId && x.IsInside)
            .GroupBy(x => x.VehicleId)
            .Select(g => new { VehicleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VehicleId, x => x.Count, cancellationToken);

        // 7. Load active usage sessions if any
        var activeSessions = await _dbContext.VehicleUsageSessions
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId) && x.TenantId == tenantId && x.Status == Domain.FleetOperations.UsageSessionStatus.Open)
            .ToDictionaryAsync(x => x.VehicleId, x => x.Id, cancellationToken);

        var results = new List<FleetMapVehicleDto>();

        foreach (var vehicle in vehicles)
        {
            telemetryStates.TryGetValue(vehicle.Id, out var tel);
            activeDriverAssignments.TryGetValue(vehicle.Id, out var driverAssign);
            activeDeviceAssignments.TryGetValue(vehicle.Id, out var deviceAssign);
            geofenceCounts.TryGetValue(vehicle.Id, out var gfCount);
            activeSessions.TryGetValue(vehicle.Id, out var sessionId);

            var device = deviceAssign?.TrackingDevice;
            var driver = driverAssign?.Driver;

            var connectivity = tel != null
                ? _connectivityService.EvaluateStatus(tel.RecordedAtUtc, offlineThreshold)
                : DeviceConnectivityStatus.Unknown;

            // Determine Marker State
            VehicleMarkerState markerState;
            if (tel == null)
            {
                markerState = VehicleMarkerState.Untracked;
            }
            else if (connectivity == DeviceConnectivityStatus.Offline)
            {
                markerState = VehicleMarkerState.Offline;
            }
            else if (tel.SpeedKph.HasValue && tel.SpeedKph.Value > 2.0m)
            {
                markerState = VehicleMarkerState.Moving;
            }
            else if (tel.IgnitionOn == true)
            {
                markerState = VehicleMarkerState.Stopped;
            }
            else
            {
                markerState = VehicleMarkerState.Parked;
            }

            // Filters on derived properties
            if (!parameters.IncludeUntracked && markerState == VehicleMarkerState.Untracked)
            {
                continue;
            }

            if (parameters.ConnectivityStatus.HasValue && connectivity != parameters.ConnectivityStatus.Value)
            {
                continue;
            }

            if (parameters.MarkerState.HasValue && markerState != parameters.MarkerState.Value)
            {
                continue;
            }

            if (parameters.IgnitionOn.HasValue && (tel == null || tel.IgnitionOn != parameters.IgnitionOn.Value))
            {
                continue;
            }

            string? makeModel = vehicle.VehicleMake != null && vehicle.VehicleModel != null
                ? $"{vehicle.VehicleMake.Name} {vehicle.VehicleModel.Name}"
                : null;

            string? driverName = driver != null
                ? $"{driver.FirstName} {driver.LastName}".Trim()
                : null;

            results.Add(new FleetMapVehicleDto(
                VehicleId: vehicle.Id,
                VehicleNumber: vehicle.VehicleNumber,
                RegistrationNumber: vehicle.RegistrationNumber,
                DisplayName: vehicle.DisplayName,
                MakeModel: makeModel,
                CategoryId: vehicle.VehicleCategoryId,
                CategoryName: vehicle.VehicleCategory?.Name,
                DriverId: driver?.Id,
                DriverName: driverName,
                DriverPhone: driver?.Phone,
                TrackingDeviceId: device?.Id,
                DeviceIdentifier: device?.DeviceIdentifier,
                Latitude: tel?.Latitude,
                Longitude: tel?.Longitude,
                SpeedKph: tel?.SpeedKph,
                HeadingDegrees: tel?.HeadingDegrees,
                IgnitionOn: tel?.IgnitionOn,
                OdometerKm: tel?.OdometerKm ?? vehicle.CurrentOdometer,
                EngineHours: tel?.EngineHours,
                BatteryVoltage: tel?.BatteryVoltage,
                FuelLevelPercent: tel?.FuelLevelPercent,
                SignalStrength: tel?.SignalStrength,
                RecordedAtUtc: tel?.RecordedAtUtc,
                ReceivedAtUtc: tel?.ReceivedAtUtc,
                ConnectivityStatus: connectivity,
                MarkerState: markerState,
                VehicleStatus: vehicle.Status,
                BranchId: vehicle.BranchId,
                BranchName: vehicle.Branch?.Name,
                CurrentUsageSessionId: sessionId != Guid.Empty ? sessionId : null,
                GeofenceCount: gfCount));
        }

        return results;
    }

    public async Task<GeoJsonFeatureCollection> GetFleetMapGeoJsonAsync(
        FleetMapQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var vehicles = await GetFleetMapVehiclesAsync(parameters, cancellationToken);
        var featureCollection = new GeoJsonFeatureCollection();

        foreach (var v in vehicles)
        {
            if (!v.Latitude.HasValue || !v.Longitude.HasValue)
                continue;

            var feature = new GeoJsonFeature
            {
                Id = v.VehicleId.ToString(),
                Geometry = new GeoJsonPointGeometry(v.Longitude.Value, v.Latitude.Value),
                Properties = new Dictionary<string, object?>
                {
                    ["vehicleId"] = v.VehicleId,
                    ["vehicleNumber"] = v.VehicleNumber,
                    ["registrationNumber"] = v.RegistrationNumber ?? "",
                    ["displayName"] = v.DisplayName,
                    ["makeModel"] = v.MakeModel ?? "",
                    ["driverName"] = v.DriverName ?? "Unassigned",
                    ["speedKph"] = v.SpeedKph.HasValue ? Math.Round(v.SpeedKph.Value, 1) : 0,
                    ["heading"] = v.HeadingDegrees.HasValue ? (int)v.HeadingDegrees.Value : 0,
                    ["ignition"] = v.IgnitionOn ?? false,
                    ["odometerKm"] = v.OdometerKm.HasValue ? Math.Round(v.OdometerKm.Value, 1) : 0,
                    ["connectivity"] = v.ConnectivityStatus.ToString(),
                    ["markerState"] = v.MarkerState.ToString(),
                    ["status"] = v.VehicleStatus.ToString(),
                    ["branchName"] = v.BranchName ?? "Main",
                    ["recordedAt"] = v.RecordedAtUtc?.ToString("o") ?? "",
                    ["geofenceCount"] = v.GeofenceCount
                }
            };

            featureCollection.Features.Add(feature);
        }

        return featureCollection;
    }

    public async Task<FleetMapVehicleDto?> GetVehicleMapStateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new FleetMapQueryParameters { IncludeUntracked = true };
        var list = await GetFleetMapVehiclesAsync(parameters, cancellationToken);
        return list.FirstOrDefault(x => x.VehicleId == vehicleId);
    }

    public async Task<IReadOnlyList<VehicleTrailPointDto>> GetVehicleTrailAsync(
        Guid vehicleId,
        FleetMapTrailQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var toUtc = parameters.ToUtc ?? DateTime.UtcNow;
        var fromUtc = parameters.FromUtc ?? toUtc.AddHours(-1);

        // Enforce max range of 7 days
        if ((toUtc - fromUtc).TotalDays > 7)
        {
            fromUtc = toUtc.AddDays(-7);
        }

        var records = await _dbContext.TelemetryRecords
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        x.VehicleId == vehicleId &&
                        x.Latitude.HasValue &&
                        x.Longitude.HasValue &&
                        x.RecordedAtUtc >= fromUtc &&
                        x.RecordedAtUtc <= toUtc)
            .OrderBy(x => x.RecordedAtUtc)
            .Take(parameters.MaxPoints)
            .Select(x => new VehicleTrailPointDto(
                x.RecordedAtUtc,
                x.Latitude!.Value,
                x.Longitude!.Value,
                x.SpeedKph,
                x.HeadingDegrees,
                x.IgnitionOn,
                x.OdometerKm))
            .ToListAsync(cancellationToken);

        return records;
    }

    public async Task<FleetMapDashboardDto> GetMapDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var allVehicles = await GetFleetMapVehiclesAsync(new FleetMapQueryParameters { IncludeUntracked = true }, cancellationToken);

        int tracked = allVehicles.Count(v => v.MarkerState != VehicleMarkerState.Untracked);
        int online = allVehicles.Count(v => v.ConnectivityStatus == DeviceConnectivityStatus.Online);
        int offline = allVehicles.Count(v => v.ConnectivityStatus == DeviceConnectivityStatus.Offline);
        int moving = allVehicles.Count(v => v.MarkerState == VehicleMarkerState.Moving);
        int stopped = allVehicles.Count(v => v.MarkerState == VehicleMarkerState.Stopped);
        int parked = allVehicles.Count(v => v.MarkerState == VehicleMarkerState.Parked);
        int untracked = allVehicles.Count(v => v.MarkerState == VehicleMarkerState.Untracked);
        int insideGf = allVehicles.Count(v => v.GeofenceCount > 0);

        var todayUtc = DateTime.UtcNow.Date;
        int entriesToday = await _dbContext.GeofenceEvents
            .CountAsync(x => x.TenantId == tenantId && x.EventType == GeofenceEventType.Entered && x.OccurredAtUtc >= todayUtc, cancellationToken);

        int exitsToday = await _dbContext.GeofenceEvents
            .CountAsync(x => x.TenantId == tenantId && x.EventType == GeofenceEventType.Exited && x.OccurredAtUtc >= todayUtc, cancellationToken);

        int activeGf = await _dbContext.Geofences
            .CountAsync(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted, cancellationToken);

        return new FleetMapDashboardDto(
            TrackedVehicles: tracked,
            OnlineVehicles: online,
            OfflineVehicles: offline,
            MovingVehicles: moving,
            StoppedVehicles: stopped,
            ParkedVehicles: parked,
            UntrackedVehicles: untracked,
            VehiclesInsideGeofences: insideGf,
            GeofenceEntriesToday: entriesToday,
            GeofenceExitsToday: exitsToday,
            ActiveGeofences: activeGf);
    }

    public Task<MapClientConfigurationDto> GetMapClientConfigurationAsync(
        CancellationToken cancellationToken = default)
    {
        var dto = new MapClientConfigurationDto(
            Provider: _mapSettings.Provider,
            StyleUrl: _mapSettings.StyleUrl,
            TileUrl: _mapSettings.TileUrl,
            Attribution: _mapSettings.Attribution,
            DefaultLatitude: _mapSettings.DefaultLatitude,
            DefaultLongitude: _mapSettings.DefaultLongitude,
            DefaultZoom: _mapSettings.DefaultZoom,
            MinimumZoom: _mapSettings.MinimumZoom,
            MaximumZoom: _mapSettings.MaximumZoom,
            IsDemoModeEnabled: _demoFleetSettings.Enabled);

        return Task.FromResult(dto);
    }
}
