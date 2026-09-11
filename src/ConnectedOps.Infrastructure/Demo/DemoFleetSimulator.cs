using System.Collections.Concurrent;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Demo;
using ConnectedOps.Application.Maps;
using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Demo;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Demo;

public sealed class DemoFleetSimulator : IDemoFleetSimulator
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITelemetryIngestionService _telemetryIngestionService;
    private readonly IAuditLogService _auditLogService;
    private readonly DemoFleetSettings _settings;
    private readonly MapSettings _mapSettings;
    private readonly ILogger<DemoFleetSimulator> _logger;

    // Simulation runtime state (per-tenant)
    private static readonly ConcurrentDictionary<Guid, bool> _tenantRunning = new();
    private static readonly ConcurrentDictionary<Guid, DateTime> _tenantLastTick = new();
    private static readonly ConcurrentDictionary<Guid, long> _tenantTicks = new();
    private static readonly ConcurrentDictionary<Guid, InternalDemoVehicleState> _vehicleStates = new();

    public DemoFleetSimulator(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ITelemetryIngestionService telemetryIngestionService,
        IAuditLogService auditLogService,
        IOptions<DemoFleetSettings> settings,
        IOptions<MapSettings> mapSettings,
        ILogger<DemoFleetSimulator> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _telemetryIngestionService = telemetryIngestionService;
        _auditLogService = auditLogService;
        _settings = settings.Value;
        _mapSettings = mapSettings.Value;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for demo simulator.");
    }

    private void EnsureDemoModeEnabled()
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Demo fleet simulator is disabled in this environment.");
        }
    }

    public Task<DemoFleetStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;
        var tenantVehicles = tenantId.HasValue
            ? _vehicleStates.Values.Where(v => v.TenantId == tenantId.Value).ToList()
            : _vehicleStates.Values.ToList();

        bool isRunning = tenantId.HasValue && _tenantRunning.TryGetValue(tenantId.Value, out var r) && r;
        DateTime? lastTick = tenantId.HasValue && _tenantLastTick.TryGetValue(tenantId.Value, out var lt) ? lt : null;
        long totalTicks = tenantId.HasValue && _tenantTicks.TryGetValue(tenantId.Value, out var tt) ? tt : 0;

        var dto = new DemoFleetStatusDto(
            IsRunning: isRunning,
            IsEnabled: _settings.Enabled,
            TotalDemoVehicles: tenantVehicles.Count,
            TotalRoutes: tenantVehicles.Select(v => v.RouteId).Distinct().Count(),
            UpdateIntervalSeconds: _settings.UpdateIntervalSeconds,
            LastTickAtUtc: lastTick,
            TotalTicksExecuted: totalTicks,
            StatusMessage: isRunning ? "Simulation active and transmitting telemetry." : "Simulation stopped.");

        return Task.FromResult(dto);
    }

    public async Task<DemoFleetStatusDto> CreateDemoFleetAsync(
        CreateDemoFleetRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureDemoModeEnabled();
        var tenantId = RequireTenantId();

        double centerLat = request.CenterLatitude ?? _settings.DefaultCenterLatitude;
        double centerLon = request.CenterLongitude ?? _settings.DefaultCenterLongitude;
        int vehicleCount = Math.Clamp(request.VehicleCount > 0 ? request.VehicleCount : _settings.VehicleCount, 1, 50);

        // 1. Ensure Vehicle category, make, model exist
        var category = await _dbContext.VehicleCategories
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "DEMO-VAN", cancellationToken);
        if (category == null)
        {
            category = new VehicleCategory(tenantId, "Demo Fleet Vehicles", "DEMO-VAN", "Automated demo fleet vehicles", true);
            _dbContext.VehicleCategories.Add(category);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var make = await _dbContext.VehicleMakes
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "DemoMotors", cancellationToken);
        if (make == null)
        {
            make = new VehicleMake(tenantId, "DemoMotors", "Global");
            _dbContext.VehicleMakes.Add(make);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var model = await _dbContext.VehicleModels
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Name == "Express Electric", cancellationToken);
        if (model == null)
        {
            model = new VehicleModel(tenantId, make.Id, "Express Electric", category.Id);
            _dbContext.VehicleModels.Add(model);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 2. Ensure Provider & Device Type
        var provider = await _dbContext.TrackingProviders
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "DEMO-TELTONIKA", cancellationToken);
        if (provider == null)
        {
            provider = new TrackingProvider("Demo GPS Provider", "DEMO-TELTONIKA", ProviderType.Teltonika, "Simulated GPS Provider", tenantId);
            _dbContext.TrackingProviders.Add(provider);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var deviceType = await _dbContext.TrackingDeviceTypes
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Code == "DEMO-TRACKER", cancellationToken);
        if (deviceType == null)
        {
            deviceType = new TrackingDeviceType("Demo Telematics Unit", "DEMO-TRACKER", "Simulated tracker", tenantId);
            _dbContext.TrackingDeviceTypes.Add(deviceType);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Create Demo Routes around center
        var routes = await GenerateDemoRoutesAsync(tenantId, centerLat, centerLon, cancellationToken);

        // 4. Create Demo Vehicles & Tracking Devices
        for (int i = 1; i <= vehicleCount; i++)
        {
            string vNumber = $"DEMO-{i:D3}";
            string regNumber = $"DEMO-{1000 + i}";
            string devIdentifier = $"DEMO-GPS-{i:D3}";
            string imei = $"86000000000{i:D4}";

            var vehicle = await _dbContext.Vehicles
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.VehicleNumber == vNumber, cancellationToken);

            if (vehicle == null)
            {
                vehicle = new Vehicle(
                    tenantId: tenantId,
                    vehicleNumber: vNumber,
                    categoryId: category.Id,
                    makeId: make.Id,
                    modelId: model.Id,
                    displayName: $"Demo Vehicle {i:D2}",
                    registrationNumber: regNumber,
                    currentOdometer: 10000m + (i * 250m),
                    status: VehicleStatus.Active);

                _dbContext.Vehicles.Add(vehicle);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var device = await _dbContext.TrackingDevices
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.DeviceIdentifier == devIdentifier, cancellationToken);

            if (device == null)
            {
                device = new TrackingDevice(
                    tenantId: tenantId,
                    deviceIdentifier: devIdentifier,
                    providerId: provider.Id,
                    deviceTypeId: deviceType.Id,
                    imei: imei,
                    name: $"Demo Tracker {i:D2}",
                    status: TrackingDeviceStatus.Online,
                    isActive: true);

                _dbContext.TrackingDevices.Add(device);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Assign device to vehicle
            var assignment = await _dbContext.TrackingDeviceVehicleAssignments
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.TrackingDeviceId == device.Id && x.IsActive, cancellationToken);

            if (assignment == null)
            {
                assignment = new TrackingDeviceVehicleAssignment(
                    tenantId: tenantId,
                    trackingDeviceId: device.Id,
                    vehicleId: vehicle.Id,
                    assignedFromUtc: DateTime.UtcNow,
                    isPrimary: true,
                    notes: "Automated Demo Vehicle Assignment");

                _dbContext.TrackingDeviceVehicleAssignments.Add(assignment);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Assign a route to vehicle state
            var route = routes[(i - 1) % routes.Count];
            var firstPoint = route.Points.OrderBy(p => p.Sequence).First();

            _vehicleStates[vehicle.Id] = new InternalDemoVehicleState
            {
                TenantId = tenantId,
                VehicleId = vehicle.Id,
                VehicleNumber = vehicle.VehicleNumber,
                DisplayName = vehicle.DisplayName,
                TrackingDeviceId = device.Id,
                DeviceIdentifier = device.DeviceIdentifier,
                RouteId = route.Id,
                RouteName = route.Name,
                CurrentPointIndex = 0,
                Points = route.Points.OrderBy(p => p.Sequence).Select(p => (p.Latitude, p.Longitude, p.SpeedKph ?? 45.0m)).ToList(),
                CurrentLatitude = firstPoint.Latitude,
                CurrentLongitude = firstPoint.Longitude,
                CurrentSpeedKph = i == 2 ? 0 : (i == 7 ? 0 : (40m + (i * 3m))), // DEMO-002 stopped, DEMO-007 parked
                CurrentHeadingDegrees = 90,
                IgnitionOn = i != 7, // DEMO-007 parked (ignition off)
                OdometerKm = vehicle.CurrentOdometer,
                BatteryPercentage = 95 - (i * 2),
                GsmSignal = 4,
                LastUpdateUtc = DateTime.UtcNow,
                IsSimulatingOffline = i == 4 // DEMO-004 starts offline simulation
            };
        }

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.DemoFleetCreated,
            "DemoFleet",
            tenantId.ToString(),
            $"Provisioned {vehicleCount} demo vehicles with simulated routes around ({centerLat:F4}, {centerLon:F4})"),
            cancellationToken);

        _logger.LogInformation("Demo fleet provisioned successfully for tenant '{TenantId}' with {Count} vehicles.", tenantId, vehicleCount);

        return await GetStatusAsync(cancellationToken);
    }

    private async Task<List<DemoRoute>> GenerateDemoRoutesAsync(
        Guid tenantId,
        double centerLat,
        double centerLon,
        CancellationToken cancellationToken)
    {
        var existingRoutes = await _dbContext.DemoRoutes
            .Include(r => r.Points)
            .Where(r => r.TenantId == tenantId && r.IsActive)
            .ToListAsync(cancellationToken);

        if (existingRoutes.Count >= 3)
        {
            return existingRoutes;
        }

        var routes = new List<DemoRoute>();

        // Route 1: Downtown Loop (North-East circuit)
        var route1 = new DemoRoute(tenantId, "Downtown Express Loop", "Circular transit loop through city center", isLoop: true);
        route1.AddPoint(1, centerLat + 0.000, centerLon + 0.000, 45m);
        route1.AddPoint(2, centerLat + 0.012, centerLon + 0.008, 55m);
        route1.AddPoint(3, centerLat + 0.020, centerLon + 0.025, 60m);
        route1.AddPoint(4, centerLat + 0.015, centerLon + 0.038, 50m);
        route1.AddPoint(5, centerLat + 0.002, centerLon + 0.032, 40m);
        route1.AddPoint(6, centerLat - 0.008, centerLon + 0.018, 45m);
        route1.AddPoint(7, centerLat - 0.005, centerLon + 0.005, 35m);
        _dbContext.DemoRoutes.Add(route1);
        routes.Add(route1);

        // Route 2: Highway Corridor (East-West corridor)
        var route2 = new DemoRoute(tenantId, "East-West Highway", "High-speed delivery corridor", isLoop: true);
        route2.AddPoint(1, centerLat - 0.015, centerLon - 0.030, 70m);
        route2.AddPoint(2, centerLat - 0.010, centerLon - 0.015, 80m);
        route2.AddPoint(3, centerLat - 0.005, centerLon + 0.000, 75m);
        route2.AddPoint(4, centerLat + 0.000, centerLon + 0.020, 80m);
        route2.AddPoint(5, centerLat + 0.008, centerLon + 0.040, 65m);
        route2.AddPoint(6, centerLat + 0.000, centerLon + 0.020, 75m);
        route2.AddPoint(7, centerLat - 0.005, centerLon + 0.000, 70m);
        route2.AddPoint(8, centerLat - 0.010, centerLon - 0.015, 80m);
        _dbContext.DemoRoutes.Add(route2);
        routes.Add(route2);

        // Route 3: Harbor Logistics Loop (South-West loop)
        var route3 = new DemoRoute(tenantId, "Harbor Logistics Route", "Industrial warehouse and dock circuit", isLoop: true);
        route3.AddPoint(1, centerLat + 0.005, centerLon - 0.005, 30m);
        route3.AddPoint(2, centerLat - 0.012, centerLon - 0.010, 35m);
        route3.AddPoint(3, centerLat - 0.025, centerLon - 0.020, 45m);
        route3.AddPoint(4, centerLat - 0.035, centerLon - 0.012, 40m);
        route3.AddPoint(5, centerLat - 0.022, centerLon + 0.005, 35m);
        route3.AddPoint(6, centerLat - 0.008, centerLon + 0.002, 30m);
        _dbContext.DemoRoutes.Add(route3);
        routes.Add(route3);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return routes;
    }

    public async Task<DemoFleetStatusDto> StartSimulationAsync(CancellationToken cancellationToken = default)
    {
        EnsureDemoModeEnabled();
        var tenantId = RequireTenantId();

        var tenantVehicles = _vehicleStates.Values.Where(v => v.TenantId == tenantId).ToList();
        if (tenantVehicles.Count == 0)
        {
            await CreateDemoFleetAsync(new CreateDemoFleetRequest(), cancellationToken);
        }

        _tenantRunning[tenantId] = true;

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.DemoSimulationStarted,
            "DemoFleet",
            tenantId.ToString(),
            "Started demo fleet live simulation"),
            cancellationToken);

        return await GetStatusAsync(cancellationToken);
    }

    public async Task<DemoFleetStatusDto> StopSimulationAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        _tenantRunning[tenantId] = false;

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.DemoSimulationStopped,
            "DemoFleet",
            tenantId.ToString(),
            "Stopped demo fleet live simulation"),
            cancellationToken);

        return await GetStatusAsync(cancellationToken);
    }

    public async Task<DemoFleetStatusDto> ResetSimulationAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        _tenantRunning[tenantId] = false;
        _tenantTicks[tenantId] = 0;

        foreach (var state in _vehicleStates.Values.Where(v => v.TenantId == tenantId))
        {
            state.CurrentPointIndex = 0;
            if (state.Points.Count > 0)
            {
                state.CurrentLatitude = state.Points[0].Latitude;
                state.CurrentLongitude = state.Points[0].Longitude;
                state.CurrentSpeedKph = state.Points[0].SpeedKph;
            }
        }

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.DemoSimulationReset,
            "DemoFleet",
            tenantId.ToString(),
            "Reset demo fleet simulation positions to route start"),
            cancellationToken);

        return await GetStatusAsync(cancellationToken);
    }

    public async Task<DemoFleetStatusDto> StepSimulationAsync(CancellationToken cancellationToken = default)
    {
        EnsureDemoModeEnabled();
        var tenantId = RequireTenantId();

        var tenantVehicles = _vehicleStates.Values.Where(v => v.TenantId == tenantId).ToList();
        if (tenantVehicles.Count == 0)
        {
            await CreateDemoFleetAsync(new CreateDemoFleetRequest(), cancellationToken);
        }

        var now = DateTime.UtcNow;
        _tenantLastTick[tenantId] = now;
        _tenantTicks.AddOrUpdate(tenantId, 1, (_, t) => t + 1);

        var targetVehicles = _vehicleStates.Values.Where(v => v.TenantId == tenantId).ToList();

        foreach (var state in targetVehicles)
        {
            if (state.IsSimulatingOffline)
            {
                // Do not transmit telemetry for offline-simulated vehicle
                continue;
            }

            if (state.Points.Count == 0)
                continue;

            // Advance waypoint if moving
            if (state.IgnitionOn && state.CurrentSpeedKph > 0)
            {
                state.CurrentPointIndex = (state.CurrentPointIndex + 1) % state.Points.Count;
                var currentPt = state.Points[state.CurrentPointIndex];
                var prevPt = state.Points[(state.CurrentPointIndex - 1 + state.Points.Count) % state.Points.Count];

                state.CurrentLatitude = currentPt.Latitude;
                state.CurrentLongitude = currentPt.Longitude;
                state.CurrentSpeedKph = currentPt.SpeedKph;

                // Calculate heading
                state.CurrentHeadingDegrees = CalculateHeading(prevPt.Latitude, prevPt.Longitude, currentPt.Latitude, currentPt.Longitude);

                // Advance odometer
                decimal deltaKm = (state.CurrentSpeedKph * (_settings.UpdateIntervalSeconds / 3600m));
                state.OdometerKm += Math.Max(0.01m, deltaKm);
            }

            state.LastUpdateUtc = now;

            // Generate normalized telemetry message
            var telemetryMessage = new NormalizedTelemetryMessage(
                DeviceIdentifier: state.DeviceIdentifier,
                Provider: "Teltonika",
                RecordedAtUtc: now,
                ReceivedAtUtc: now,
                Latitude: state.CurrentLatitude,
                Longitude: state.CurrentLongitude,
                AltitudeMeters: 15,
                SpeedKph: state.CurrentSpeedKph,
                HeadingDegrees: state.CurrentHeadingDegrees,
                IgnitionOn: state.IgnitionOn,
                OdometerKm: state.OdometerKm,
                EngineHours: 120.5m + (_tenantTicks.GetValueOrDefault(tenantId, 1) * 0.001m),
                BatteryVoltage: 12.6m,
                ExternalPowerVoltage: 13.8m,
                GsmSignal: state.GsmSignal,
                GpsSatellites: 12,
                EventType: state.IgnitionOn ? TelemetryEventType.Periodic : TelemetryEventType.IgnitionOff);

            // Ingest through Phase 6 pipeline
            try
            {
                await _telemetryIngestionService.IngestAsync(telemetryMessage, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Demo simulation failed to ingest telemetry for device '{DeviceIdentifier}'", state.DeviceIdentifier);
            }
        }

        return await GetStatusAsync(cancellationToken);
    }

    public Task<IReadOnlyCollection<DemoVehicleStateDto>> GetDemoVehiclesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;
        var query = tenantId.HasValue
            ? _vehicleStates.Values.Where(v => v.TenantId == tenantId.Value)
            : _vehicleStates.Values;

        var list = query.Select(v => new DemoVehicleStateDto(
            VehicleId: v.VehicleId,
            VehicleNumber: v.VehicleNumber,
            DisplayName: v.DisplayName,
            TrackingDeviceId: v.TrackingDeviceId,
            DeviceIdentifier: v.DeviceIdentifier,
            RouteId: v.RouteId,
            RouteName: v.RouteName,
            CurrentPointIndex: v.CurrentPointIndex,
            TotalRoutePoints: v.Points.Count,
            Latitude: v.CurrentLatitude,
            Longitude: v.CurrentLongitude,
            SpeedKph: v.CurrentSpeedKph,
            HeadingDegrees: v.CurrentHeadingDegrees,
            IgnitionOn: v.IgnitionOn,
            OdometerKm: v.OdometerKm,
            BatteryPercentage: v.BatteryPercentage,
            GsmSignal: v.GsmSignal,
            LastUpdateUtc: v.LastUpdateUtc,
            IsSimulatingOffline: v.IsSimulatingOffline)).ToList();

        return Task.FromResult<IReadOnlyCollection<DemoVehicleStateDto>>(list);
    }

    public Task<bool> ToggleVehicleOfflineSimulationAsync(Guid vehicleId, CancellationToken cancellationToken = default)
    {
        if (_vehicleStates.TryGetValue(vehicleId, out var state))
        {
            state.IsSimulatingOffline = !state.IsSimulatingOffline;
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    private static decimal CalculateHeading(double lat1, double lon1, double lat2, double lon2)
    {
        double dLon = (lon2 - lon1) * Math.PI / 180.0;
        double rLat1 = lat1 * Math.PI / 180.0;
        double rLat2 = lat2 * Math.PI / 180.0;

        double y = Math.Sin(dLon) * Math.Cos(rLat2);
        double x = Math.Cos(rLat1) * Math.Sin(rLat2) - Math.Sin(rLat1) * Math.Cos(rLat2) * Math.Cos(dLon);

        double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
        double heading = (brng + 360.0) % 360.0;

        return (decimal)Math.Round(heading, 1);
    }

    private sealed class InternalDemoVehicleState
    {
        public Guid TenantId { get; set; }
        public Guid VehicleId { get; set; }
        public string VehicleNumber { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public Guid TrackingDeviceId { get; set; }
        public string DeviceIdentifier { get; set; } = null!;
        public Guid RouteId { get; set; }
        public string RouteName { get; set; } = null!;
        public int CurrentPointIndex { get; set; }
        public List<(double Latitude, double Longitude, decimal SpeedKph)> Points { get; set; } = [];
        public double CurrentLatitude { get; set; }
        public double CurrentLongitude { get; set; }
        public decimal CurrentSpeedKph { get; set; }
        public decimal CurrentHeadingDegrees { get; set; }
        public bool IgnitionOn { get; set; }
        public decimal OdometerKm { get; set; }
        public int BatteryPercentage { get; set; }
        public int GsmSignal { get; set; }
        public DateTime LastUpdateUtc { get; set; }
        public bool IsSimulatingOffline { get; set; }
    }
}
