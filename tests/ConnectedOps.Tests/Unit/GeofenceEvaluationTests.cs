using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Geofences;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class GeofenceEvaluationTests
{
    private static async Task<(Vehicle Vehicle, Geofence Geofence)> SetupVehicleAndCircleGeofenceAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        double centerLat = 25.2048,
        double centerLon = 55.2708,
        double radiusMeters = 500)
    {
        var category = new VehicleCategory(tenantId, "Van", "VAN", null, true);
        var make = new VehicleMake(tenantId, "Ford", "US");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Transit", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, "VAN-101", category.Id, make.Id, model.Id, displayName: "Ford Transit Van");
        db.Vehicles.Add(vehicle);

        var geofence = new Geofence(tenantId, "HQ Depot", "GEO-HQ", GeofenceType.Circle, null, centerLat, centerLon, radiusMeters);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        return (vehicle, geofence);
    }

    [Fact]
    public async Task EvaluatePosition_OutsideToInside_EmitsEnteredEventAndUpdatesState()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, geofence) = await SetupVehicleAndCircleGeofenceAsync(db, tenantId);

        var service = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        // Vehicle moves inside circle (at 25.2048, 55.2708)
        var recordedAt = DateTime.UtcNow;
        await service.EvaluatePositionAsync(
            tenantId,
            vehicle.Id,
            latitude: 25.2048,
            longitude: 55.2708,
            recordedAtUtc: recordedAt,
            receivedAtUtc: recordedAt);

        // Verify Event
        var events = await db.GeofenceEvents.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Single(events);
        Assert.Equal(GeofenceEventType.Entered, events[0].EventType);
        Assert.Equal(geofence.Id, events[0].GeofenceId);
        Assert.Equal(vehicle.Id, events[0].VehicleId);

        // Verify Presence State
        var state = await db.VehicleGeofenceStates.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.VehicleId == vehicle.Id && x.GeofenceId == geofence.Id);
        Assert.NotNull(state);
        Assert.True(state.IsInside);
        Assert.Equal(recordedAt, state.LastEnteredAtUtc);
    }

    [Fact]
    public async Task EvaluatePosition_ContinuousInside_DoesNotEmitDuplicateEnteredEvents()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, geofence) = await SetupVehicleAndCircleGeofenceAsync(db, tenantId);

        var service = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        // First position inside
        var t1 = DateTime.UtcNow.AddMinutes(-5);
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.2048, 55.2708, t1, t1);

        // Second position still inside
        var t2 = DateTime.UtcNow;
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.2050, 55.2710, t2, t2);

        // Should still only have 1 event
        var events = await db.GeofenceEvents.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Single(events);
        Assert.Equal(GeofenceEventType.Entered, events[0].EventType);

        // State remains inside
        var state = await db.VehicleGeofenceStates.FirstOrDefaultAsync(x => x.VehicleId == vehicle.Id);
        Assert.NotNull(state);
        Assert.True(state.IsInside);
        Assert.Equal(t2, state.LastEvaluatedAtUtc);
    }

    [Fact]
    public async Task EvaluatePosition_InsideToOutside_EmitsExitedEventAndUpdatesState()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, geofence) = await SetupVehicleAndCircleGeofenceAsync(db, tenantId);

        var service = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        // 1. Move inside
        var t1 = DateTime.UtcNow.AddMinutes(-10);
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.2048, 55.2708, t1, t1);

        // 2. Move outside (10km away)
        var t2 = DateTime.UtcNow;
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.3048, 55.3708, t2, t2);

        // Should have 2 events: Entered then Exited
        var events = await db.GeofenceEvents.Where(x => x.TenantId == tenantId).OrderBy(x => x.OccurredAtUtc).ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Equal(GeofenceEventType.Entered, events[0].EventType);
        Assert.Equal(GeofenceEventType.Exited, events[1].EventType);

        // State is now outside
        var state = await db.VehicleGeofenceStates.FirstOrDefaultAsync(x => x.VehicleId == vehicle.Id);
        Assert.NotNull(state);
        Assert.False(state.IsInside);
        Assert.Equal(t2, state.LastExitedAtUtc);
    }

    [Fact]
    public async Task EvaluatePosition_PolygonGeofence_DetectsTransitions()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();

        var category = new VehicleCategory(tenantId, "Truck", "TRK", null, true);
        var make = new VehicleMake(tenantId, "MAN", "DE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "TGX", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, "TRK-202", category.Id, make.Id, model.Id, displayName: "MAN Truck");
        db.Vehicles.Add(vehicle);

        string polygonGeoJson = "{\"type\":\"Polygon\",\"coordinates\":[[[55.270,25.200],[55.280,25.200],[55.280,25.210],[55.270,25.210],[55.270,25.200]]]}";
        var polygonGeofence = new Geofence(tenantId, "Industrial Zone", "GEO-POLY-01", GeofenceType.Polygon, null, 25.205, 55.275, null, polygonGeoJson);
        db.Geofences.Add(polygonGeofence);
        await db.SaveChangesAsync();

        var service = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        // Inside polygon point (25.205, 55.275)
        var t1 = DateTime.UtcNow;
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.205, 55.275, t1, t1);

        var events = await db.GeofenceEvents.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Single(events);
        Assert.Equal(GeofenceEventType.Entered, events[0].EventType);
    }

    [Fact]
    public async Task EvaluatePosition_InactiveGeofence_IsIgnored()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var (vehicle, geofence) = await SetupVehicleAndCircleGeofenceAsync(db, tenantId);

        // Deactivate geofence
        geofence.Deactivate();
        await db.SaveChangesAsync();

        var service = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        var t1 = DateTime.UtcNow;
        await service.EvaluatePositionAsync(tenantId, vehicle.Id, 25.2048, 55.2708, t1, t1);

        var events = await db.GeofenceEvents.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Empty(events);

        var states = await db.VehicleGeofenceStates.Where(x => x.TenantId == tenantId).ToListAsync();
        Assert.Empty(states);
    }
}
