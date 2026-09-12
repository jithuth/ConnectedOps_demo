using ConnectedOps.Application.Demo;
using ConnectedOps.Application.Maps;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Demo;
using ConnectedOps.Infrastructure.Geofences;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Telematics;
using ConnectedOps.Infrastructure.Vehicles;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class DemoFleetSimulatorTests
{
    private static (DemoFleetSimulator Simulator, ConnectedOpsDbContext Db, TelemetryIngestionService Ingestion) CreateSimulator(
        Guid tenantId,
        bool enabled = true)
    {
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var validationService = new TelemetryValidationService();
        var dedupService = new TelemetryDeduplicationService(db);
        var odoService = new VehicleOdometerService(db, userContext, auditService);
        var geofenceEval = new GeofenceEvaluationService(db, NullLogger<GeofenceEvaluationService>.Instance);

        var ingestionService = new TelemetryIngestionService(
            db,
            validationService,
            dedupService,
            odoService,
            NullLogger<TelemetryIngestionService>.Instance,
            geofenceEval);

        var demoSettings = Options.Create(new DemoFleetSettings { Enabled = enabled });
        var mapSettings = Options.Create(new MapSettings());

        var simulator = new DemoFleetSimulator(
            db,
            userContext,
            ingestionService,
            auditService,
            demoSettings,
            mapSettings,
            NullLogger<DemoFleetSimulator>.Instance);

        return (simulator, db, ingestionService);
    }

    [Fact]
    public async Task CreateDemoFleetAsync_Generates10VehiclesAndRoutes()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, db, _) = CreateSimulator(tenantId, enabled: true);

        var status = await simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest { VehicleCount = 10 });

        Assert.NotNull(status);
        Assert.Equal(10, status.TotalDemoVehicles);

        var vehicles = await db.Vehicles.Where(v => v.TenantId == tenantId && v.VehicleNumber.StartsWith("DEMO-")).ToListAsync();
        Assert.Equal(10, vehicles.Count);

        var devices = await db.TrackingDevices.Where(d => d.TenantId == tenantId && d.DeviceIdentifier.StartsWith("DEMO-GPS-")).ToListAsync();
        Assert.Equal(10, devices.Count);

        var routes = await db.DemoRoutes.Where(r => r.TenantId == tenantId).ToListAsync();
        Assert.Equal(3, routes.Count);

        var demoStates = await simulator.GetDemoVehiclesAsync();
        Assert.Equal(10, demoStates.Count);
    }

    [Fact]
    public async Task StepSimulationAsync_DispatchesTelemetryThroughIngestionPipeline()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, db, _) = CreateSimulator(tenantId, enabled: true);

        // 1. Create Fleet
        await simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest { VehicleCount = 10 });

        // 2. Step 1 Tick
        var status = await simulator.StepSimulationAsync();

        Assert.NotNull(status);
        Assert.Equal(1, status.TotalTicksExecuted);

        // 3. Verify TelemetryRecords and VehicleTelemetryStates were populated by the ingestion service!
        // 9 active online vehicles transmit telemetry while 1 is in simulated offline mode
        var records = await db.TelemetryRecords.Where(r => r.TenantId == tenantId).ToListAsync();
        Assert.Equal(9, records.Count);

        var telemetryStates = await db.VehicleTelemetryStates.Where(s => s.TenantId == tenantId).ToListAsync();
        Assert.Equal(9, telemetryStates.Count);

        // Verify each state has valid coordinates and speeds
        foreach (var state in telemetryStates)
        {
            Assert.True(state.Latitude != 0);
            Assert.True(state.Longitude != 0);
            Assert.NotNull(state.SpeedKph);
            Assert.NotNull(state.HeadingDegrees);
        }
    }

    [Fact]
    public async Task StepSimulationAsync_TriggersGeofenceEvaluationAndDetectsEntry()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, db, _) = CreateSimulator(tenantId, enabled: true);

        // Create fleet
        await simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest { VehicleCount = 10 });

        // Create a large geofence covering Dubai area (radius 50km)
        var geofence = new Geofence(tenantId, "Greater Dubai Zone", "GEO-DXB-ALL", GeofenceType.Circle, null, 25.2048, 55.2708, 50000);
        db.Geofences.Add(geofence);
        await db.SaveChangesAsync();

        // Step simulation tick
        await simulator.StepSimulationAsync();

        // Check that geofence events and presence were created automatically by the ingestion pipeline
        var events = await db.GeofenceEvents.Where(e => e.TenantId == tenantId).ToListAsync();
        Assert.NotEmpty(events);
        Assert.All(events, e => Assert.Equal(GeofenceEventType.Entered, e.EventType));

        var presences = await db.VehicleGeofenceStates.Where(p => p.TenantId == tenantId && p.IsInside).ToListAsync();
        Assert.NotEmpty(presences);
    }

    [Fact]
    public async Task ToggleVehicleOfflineSimulationAsync_SimulatesOfflineBehavior()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, db, _) = CreateSimulator(tenantId, enabled: true);

        await simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest { VehicleCount = 10 });
        var demoVehicles = await simulator.GetDemoVehiclesAsync();
        var firstVehicle = demoVehicles.First(v => !v.IsSimulatingOffline);

        // Toggle offline
        var isOffline = await simulator.ToggleVehicleOfflineSimulationAsync(firstVehicle.VehicleId);
        Assert.True(isOffline);

        var updatedStates = await simulator.GetDemoVehiclesAsync();
        var updatedVehicle = updatedStates.First(v => v.VehicleId == firstVehicle.VehicleId);
        Assert.True(updatedVehicle.IsSimulatingOffline);

        // Step simulation tick - offline vehicle should not emit new telemetry
        await simulator.StepSimulationAsync();

        var recordsForOffline = await db.TelemetryRecords.Where(r => r.VehicleId == firstVehicle.VehicleId).ToListAsync();
        Assert.Empty(recordsForOffline);
    }

    [Fact]
    public async Task ResetSimulationAsync_ResetsVehiclesToWaypointZero()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, db, _) = CreateSimulator(tenantId, enabled: true);

        await simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest { VehicleCount = 10 });

        // Step several ticks
        await simulator.StepSimulationAsync();
        await simulator.StepSimulationAsync();

        // Reset
        var status = await simulator.ResetSimulationAsync();
        Assert.NotNull(status);

        var states = await simulator.GetDemoVehiclesAsync();
        Assert.All(states, s => Assert.Equal(0, s.CurrentPointIndex));
    }

    [Fact]
    public async Task Simulator_DisabledWhenFeatureFlagIsFalse_ThrowsException()
    {
        var tenantId = Guid.NewGuid();
        var (simulator, _, _) = CreateSimulator(tenantId, enabled: false);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest()));
    }
}
