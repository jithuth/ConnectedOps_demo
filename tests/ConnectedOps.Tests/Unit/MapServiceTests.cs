using ConnectedOps.Application.Maps;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Maps;
using ConnectedOps.Infrastructure.Telematics;
using ConnectedOps.Tests.Common;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class MapServiceTests
{
    private static async Task<(Vehicle Vehicle, TrackingDevice Device, VehicleTelemetryState State)> SetupTrackedVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "FLT-MAP-01",
        double latitude = 25.2048,
        double longitude = 55.2708,
        decimal speedKph = 65.5m,
        decimal headingDegrees = 180m,
        bool ignitionOn = true)
    {
        var category = new VehicleCategory(tenantId, "Van", "VAN", null, true);
        var make = new VehicleMake(tenantId, "Toyota", "JP");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "HiAce", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, vehicleNumber, category.Id, make.Id, model.Id, displayName: "Express Delivery Van");
        db.Vehicles.Add(vehicle);

        var provider = new TrackingProvider("Teltonika", "TEL", ProviderType.Teltonika, "GPS", tenantId);
        var deviceType = new TrackingDeviceType("FMC130", "FMC130", "Tracker", tenantId);
        db.TrackingProviders.Add(provider);
        db.TrackingDeviceTypes.Add(deviceType);
        await db.SaveChangesAsync();

        var device = new TrackingDevice(tenantId, "DEV-" + vehicleNumber, provider.Id, deviceType.Id, imei: "123456789012345", serialNumber: "SN123");
        db.TrackingDevices.Add(device);
        await db.SaveChangesAsync();

        var assignment = new TrackingDeviceVehicleAssignment(tenantId, device.Id, vehicle.Id, DateTime.UtcNow, isPrimary: true);
        db.TrackingDeviceVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var state = new VehicleTelemetryState(
            vehicle.Id,
            tenantId,
            device.Id,
            recordedAtUtc: DateTime.UtcNow,
            receivedAtUtc: DateTime.UtcNow,
            latitude: latitude,
            longitude: longitude,
            altitudeMeters: 15,
            speedKph: speedKph,
            headingDegrees: headingDegrees,
            ignitionOn: ignitionOn,
            odometerKm: 12500m,
            batteryVoltage: 12.6m,
            fuelLevelPercent: 85m);

        db.VehicleTelemetryStates.Add(state);
        await db.SaveChangesAsync();

        return (vehicle, device, state);
    }

    [Fact]
    public async Task GetFleetMapVehiclesAsync_ReturnsTrackedVehiclesWithProjections()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, device, state) = await SetupTrackedVehicleAsync(db, tenantId);

        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var connService = new DeviceConnectivityService(db);
        var mapOptions = Options.Create(new MapSettings());
        var demoOptions = Options.Create(new DemoFleetSettings());

        var service = new FleetMapService(db, userContext, connService, mapOptions, demoOptions);

        var results = await service.GetFleetMapVehiclesAsync(new FleetMapQueryParameters());

        Assert.Single(results);
        var item = results.First();
        Assert.Equal(vehicle.Id, item.VehicleId);
        Assert.Equal("FLT-MAP-01", item.VehicleNumber);
        Assert.Equal(25.2048, item.Latitude);
        Assert.Equal(55.2708, item.Longitude);
        Assert.Equal(65.5m, item.SpeedKph);
        Assert.Equal(180m, item.HeadingDegrees);
        Assert.True(item.IgnitionOn);
        Assert.Equal(VehicleMarkerState.Moving, item.MarkerState);
    }

    [Fact]
    public async Task GetFleetMapGeoJsonAsync_ReturnsRFC7946CompliantFeatureCollection()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, device, state) = await SetupTrackedVehicleAsync(db, tenantId, latitude: 25.2048, longitude: 55.2708);

        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var connService = new DeviceConnectivityService(db);
        var mapOptions = Options.Create(new MapSettings());
        var demoOptions = Options.Create(new DemoFleetSettings());

        var service = new FleetMapService(db, userContext, connService, mapOptions, demoOptions);

        var geoJson = await service.GetFleetMapGeoJsonAsync(new FleetMapQueryParameters());

        Assert.NotNull(geoJson);
        Assert.Equal("FeatureCollection", geoJson.Type);
        Assert.Single(geoJson.Features);

        var feature = geoJson.Features[0];
        Assert.Equal("Feature", feature.Type);
        Assert.Equal("Point", feature.Geometry.Type);

        // Crucial: GeoJSON RFC 7946 specifies [longitude, latitude]
        Assert.Equal(55.2708, feature.Geometry.Coordinates[0]);
        Assert.Equal(25.2048, feature.Geometry.Coordinates[1]);

        Assert.Equal("FLT-MAP-01", feature.Properties["vehicleNumber"]);
    }

    [Fact]
    public async Task GetVehicleTrailAsync_ReturnsChronologicalBreadcrumbTrail()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, device, state) = await SetupTrackedVehicleAsync(db, tenantId);

        // Add Telemetry Records along a route
        var t0 = DateTime.UtcNow.AddMinutes(-10);
        for (int i = 0; i < 5; i++)
        {
            var record = new TelemetryRecord(
                tenantId,
                device.Id,
                recordedAtUtc: t0.AddMinutes(i * 2),
                receivedAtUtc: t0.AddMinutes(i * 2).AddSeconds(1),
                sourceProvider: "TELTONIKA",
                vehicleId: vehicle.Id,
                latitude: 25.2000 + (i * 0.001),
                longitude: 55.2700 + (i * 0.001),
                altitudeMeters: 10,
                speedKph: 40 + i * 5,
                headingDegrees: 90,
                ignitionOn: true,
                odometerKm: 12500 + (i * 0.5m),
                batteryVoltage: 12.6m,
                fuelLevelPercent: 80);
            db.TelemetryRecords.Add(record);
        }
        await db.SaveChangesAsync();

        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var connService = new DeviceConnectivityService(db);
        var mapOptions = Options.Create(new MapSettings());
        var demoOptions = Options.Create(new DemoFleetSettings());

        var service = new FleetMapService(db, userContext, connService, mapOptions, demoOptions);

        var trail = await service.GetVehicleTrailAsync(vehicle.Id, new FleetMapTrailQueryParameters { MaxPoints = 100 });

        Assert.Equal(5, trail.Count);
        Assert.Equal(25.2000, trail[0].Latitude, 4);
        Assert.Equal(25.2040, trail[4].Latitude, 4);
    }

    [Fact]
    public async Task GetMapDashboardMetricsAsync_CalculatesKPIsCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, device, state) = await SetupTrackedVehicleAsync(db, tenantId);

        var geofence = new Geofence(tenantId, "HQ", "GEO-HQ", GeofenceType.Circle, null, 25.2048, 55.2708, 500);
        db.Geofences.Add(geofence);

        var presence = new VehicleGeofenceState(tenantId, vehicle.Id, geofence.Id, isInside: true, lastEvaluatedAtUtc: DateTime.UtcNow, lastEnteredAtUtc: DateTime.UtcNow);
        db.VehicleGeofenceStates.Add(presence);
        await db.SaveChangesAsync();

        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var connService = new DeviceConnectivityService(db);
        var mapOptions = Options.Create(new MapSettings());
        var demoOptions = Options.Create(new DemoFleetSettings());

        var service = new FleetMapService(db, userContext, connService, mapOptions, demoOptions);

        var metrics = await service.GetMapDashboardMetricsAsync();

        Assert.Equal(1, metrics.TrackedVehicles);
        Assert.Equal(1, metrics.OnlineVehicles);
        Assert.Equal(1, metrics.MovingVehicles);
        Assert.Equal(0, metrics.StoppedVehicles);
        Assert.Equal(1, metrics.VehiclesInsideGeofences);
        Assert.Equal(1, metrics.ActiveGeofences);
    }

    [Fact]
    public async Task MapService_EnforcesStrictTenantIsolation()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var db = TestDbContextFactory.Create();

        await SetupTrackedVehicleAsync(db, tenant1, "FLT-T1");
        await SetupTrackedVehicleAsync(db, tenant2, "FLT-T2");

        var userContext = new TestUserContext { TenantId = tenant1, UserId = Guid.NewGuid() };
        var connService = new DeviceConnectivityService(db);
        var mapOptions = Options.Create(new MapSettings());
        var demoOptions = Options.Create(new DemoFleetSettings());

        var service = new FleetMapService(db, userContext, connService, mapOptions, demoOptions);

        var vehicles = await service.GetFleetMapVehiclesAsync(new FleetMapQueryParameters());

        Assert.Single(vehicles);
        Assert.Equal("FLT-T1", vehicles.First().VehicleNumber);
    }
}
