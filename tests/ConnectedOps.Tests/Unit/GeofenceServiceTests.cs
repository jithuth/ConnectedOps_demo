using ConnectedOps.Application.Geofences;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Geofences;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class GeofenceServiceTests
{
    [Fact]
    public async Task CreateGeofenceAsync_CircleGeofence_CreatesSuccessfully()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var service = new GeofenceService(db, userContext, auditService);

        var request = new CreateGeofenceRequest(
            Name: "Dubai Depot",
            Code: "GEO-DXB-01",
            GeofenceType: GeofenceType.Circle,
            Description: "Main depot perimeter",
            CenterLatitude: 25.2048,
            CenterLongitude: 55.2708,
            RadiusMeters: 500,
            ColorHex: "#3b82f6");

        var result = await service.CreateGeofenceAsync(request);

        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Dubai Depot", result.Name);
        Assert.Equal("GEO-DXB-01", result.Code);
        Assert.Equal(GeofenceType.Circle, result.GeofenceType);
        Assert.Equal(500, result.RadiusMeters);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateGeofenceAsync_PolygonGeofence_CreatesSuccessfully()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var service = new GeofenceService(db, userContext, auditService);

        string polygonGeoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[55.27,25.20],[55.28,25.20],[55.28,25.21],[55.27,25.21],[55.27,25.20]]]}";

        var request = new CreateGeofenceRequest(
            Name: "Jebel Ali Industrial Zone",
            Code: "GEO-JA-01",
            GeofenceType: GeofenceType.Polygon,
            Description: "Custom polygon zone",
            CenterLatitude: 25.205,
            CenterLongitude: 55.275,
            PolygonGeoJson: polygonGeoJson,
            ColorHex: "#8b5cf6");

        var result = await service.CreateGeofenceAsync(request);

        Assert.NotNull(result);
        Assert.Equal(GeofenceType.Polygon, result.GeofenceType);
        Assert.Equal(polygonGeoJson, result.PolygonGeoJson);
    }

    [Fact]
    public async Task UpdateGeofenceAsync_UpdatesProperties()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var service = new GeofenceService(db, userContext, auditService);

        var geofence = new Geofence(tenantId, "Original Name", "GEO-ORIG", GeofenceType.Circle, null, 25.20, 55.27, 400);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        var updateReq = new UpdateGeofenceRequest(
            Name: "Updated Name",
            GeofenceType: GeofenceType.Circle,
            Description: "Updated desc",
            CenterLatitude: 25.25,
            CenterLongitude: 55.30,
            RadiusMeters: 750,
            ColorHex: "#ef4444");

        var updated = await service.UpdateGeofenceAsync(geofence.Id, updateReq);

        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal(750, updated.RadiusMeters);
        Assert.Equal("#ef4444", updated.ColorHex);
    }

    [Fact]
    public async Task DeactivateAndActivateGeofenceAsync_TogglesActiveState()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var service = new GeofenceService(db, userContext, auditService);

        var geofence = new Geofence(tenantId, "Test Zone", "GEO-TEST", GeofenceType.Circle, null, 25.20, 55.27, 300);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        var deactSuccess = await service.DeactivateGeofenceAsync(geofence.Id);
        Assert.True(deactSuccess);

        var stateAfterDeact = await service.GetGeofenceByIdAsync(geofence.Id);
        Assert.NotNull(stateAfterDeact);
        Assert.False(stateAfterDeact.IsActive);

        var actSuccess = await service.ActivateGeofenceAsync(geofence.Id);
        Assert.True(actSuccess);

        var stateAfterAct = await service.GetGeofenceByIdAsync(geofence.Id);
        Assert.NotNull(stateAfterAct);
        Assert.True(stateAfterAct.IsActive);
    }

    [Fact]
    public async Task DeleteGeofenceAsync_SoftDeletesGeofence()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var service = new GeofenceService(db, userContext, auditService);

        var geofence = new Geofence(tenantId, "To Delete", "GEO-DEL", GeofenceType.Circle, null, 25.20, 55.27, 300);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        var deleted = await service.DeleteGeofenceAsync(geofence.Id);
        Assert.True(deleted);

        var fetched = await service.GetGeofenceByIdAsync(geofence.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetGeofencesPagedAsync_EnforcesTenantIsolation()
    {
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var auditService = new TestAuditLogService();

        var geo1 = new Geofence(tenant1, "Tenant 1 Zone", "GEO-T1", GeofenceType.Circle, null, 25.20, 55.27, 300);
        var geo2 = new Geofence(tenant2, "Tenant 2 Zone", "GEO-T2", GeofenceType.Circle, null, 25.20, 55.27, 300);
        db.Geofences.AddRange(geo1, geo2);
        await db.SaveChangesAsync();

        var userContext = new TestUserContext { TenantId = tenant1, UserId = Guid.NewGuid() };
        var service = new GeofenceService(db, userContext, auditService);

        var results = await service.GetGeofencesPagedAsync(new GeofenceQueryParameters());

        Assert.Single(results);
        Assert.Equal(geo1.Id, results.First().Id);
    }

    [Fact]
    public async Task GetVehiclesInsideGeofenceAsync_ReturnsVehiclesWithActivePresence()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var category = new VehicleCategory(tenantId, "Sedan", "SED", null, true);
        var make = new VehicleMake(tenantId, "Toyota", "JP");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Camry", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, "FLT-001", category.Id, make.Id, model.Id, displayName: "Fleet Car 1");
        db.Vehicles.Add(vehicle);

        var geofence = new Geofence(tenantId, "HQ Depot", "GEO-HQ", GeofenceType.Circle, null, 25.20, 55.27, 500);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        var presence = new VehicleGeofenceState(tenantId, vehicle.Id, geofence.Id, isInside: true, lastEvaluatedAtUtc: DateTime.UtcNow, lastEnteredAtUtc: DateTime.UtcNow.AddMinutes(-30));
        db.VehicleGeofenceStates.Add(presence);
        await db.SaveChangesAsync();

        var service = new GeofenceService(db, userContext, auditService);
        var presentVehicles = await service.GetVehiclesInsideGeofenceAsync(geofence.Id);

        Assert.Single(presentVehicles);
        Assert.Equal(vehicle.Id, presentVehicles.First().VehicleId);
        Assert.Equal("FLT-001", presentVehicles.First().VehicleNumber);
    }
}
