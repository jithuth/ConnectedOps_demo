using ConnectedOps.Api.Hubs;
using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Predictive;
using ConnectedOps.Infrastructure.Demo;
using ConnectedOps.Tests.Common;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class DemoSeederAndSignalRTests
{
    // =========================================================================
    // PILLAR 1: MASTER ENTERPRISE DEMO SEEDER TESTS
    // =========================================================================

    [Fact]
    public async Task MasterEnterpriseDemoSeeder_SeedsAllEnterpriseDataSuccessfully()
    {
        using var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext();
        var seeder = new MasterEnterpriseDemoSeeder(db, userContext, NullLogger<MasterEnterpriseDemoSeeder>.Instance);

        // Execute seeding
        var result = await seeder.SeedEnterpriseDemoDataAsync();

        // Assertions on result DTO
        Assert.Equal("Apex Global Logistics", result.TenantName);
        Assert.Equal(4, result.VehiclesCreated);
        Assert.Equal(3, result.EvStationsCreated);
        Assert.Equal(2, result.ChargingSessionsCreated);
        Assert.Equal(1, result.VrpRunsCreated);
        Assert.Equal(5, result.SubsystemsEvaluated);
        Assert.True(result.PredictiveAlertsCreated >= 1);
        Assert.True(result.BrandingConfigured);
        Assert.Equal("fleet.apexlogistics.com", result.CustomDomainRegistered);
        Assert.False(string.IsNullOrWhiteSpace(result.AuditPackageNumber));

        // Verify entities persisted in database
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == result.TenantId);
        Assert.NotNull(tenant);
        Assert.Equal("Apex Global Logistics", tenant.Name);

        var vehicles = await db.Vehicles.Where(v => v.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(4, vehicles.Count);

        var stations = await db.ChargingStations.Where(s => s.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(3, stations.Count);

        var batteries = await db.VehicleBatteryStates.Where(b => b.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(3, batteries.Count); // 3 EVs have battery states

        var stopSequences = await db.OptimizedStopSequences.Where(s => s.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(8, stopSequences.Count);

        var branding = await db.TenantBrandings.FirstOrDefaultAsync(b => b.TenantId == result.TenantId);
        Assert.NotNull(branding);
        Assert.Equal("#1e3a8a", branding.PrimaryAccentColor);

        var domain = await db.TenantCustomDomains.FirstOrDefaultAsync(d => d.TenantId == result.TenantId);
        Assert.NotNull(domain);
        Assert.Equal("fleet.apexlogistics.com", domain.Hostname);

        var compliance = await db.AuditCompliancePackages.FirstOrDefaultAsync(c => c.TenantId == result.TenantId);
        Assert.NotNull(compliance);
        Assert.Equal("AUDIT-SOC2-APEX-01", compliance.PackageNumber);

        var subsystems = await db.VehicleSubsystemHealths.Where(t => t.TenantId == result.TenantId).ToListAsync();
        Assert.Equal(5, subsystems.Count);

        var alerts = await db.PredictiveMaintenanceAlerts.Where(a => a.TenantId == result.TenantId).ToListAsync();
        Assert.NotEmpty(alerts);
    }

    [Fact]
    public async Task MasterEnterpriseDemoSeeder_IsIdempotentOnRepeatedRuns()
    {
        using var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext();
        var seeder = new MasterEnterpriseDemoSeeder(db, userContext, NullLogger<MasterEnterpriseDemoSeeder>.Instance);

        // First run
        var firstResult = await seeder.SeedEnterpriseDemoDataAsync();
        Assert.Equal(4, firstResult.VehiclesCreated);

        // Second run with explicit target tenant
        var secondResult = await seeder.SeedEnterpriseDemoDataAsync(firstResult.TenantId);
        Assert.Equal(firstResult.TenantId, secondResult.TenantId);

        // Count should not duplicate vehicles
        var vehicleCount = await db.Vehicles.CountAsync(v => v.TenantId == firstResult.TenantId);
        Assert.Equal(4, vehicleCount);
    }

    // =========================================================================
    // PILLAR 2: REAL-TIME SIGNALR FLEET HUB TESTS
    // =========================================================================

    [Fact]
    public async Task FleetHub_JoinAndLeaveTenantGroup_HandlesValidAndInvalidGuids()
    {
        var mockGroups = new TestGroupManager();
        var mockClients = new TestHubCallerClients();
        var hub = new FleetHub(NullLogger<FleetHub>.Instance)
        {
            Context = new TestHubCallerContext("conn-12345"),
            Groups = mockGroups,
            Clients = mockClients
        };

        var validTenantId = Guid.NewGuid().ToString();

        // 1. Join with valid GUID
        await hub.JoinTenantGroup(validTenantId);
        Assert.Contains($"tenant-{validTenantId}", mockGroups.AddedGroups);

        // 2. Leave with valid GUID
        await hub.LeaveTenantGroup(validTenantId);
        Assert.Contains($"tenant-{validTenantId}", mockGroups.RemovedGroups);

        // 3. Invalid GUID should be ignored gracefully
        var initialAddCount = mockGroups.AddedGroups.Count;
        await hub.JoinTenantGroup("invalid-not-a-guid");
        Assert.Equal(initialAddCount, mockGroups.AddedGroups.Count);
    }

    [Fact]
    public async Task FleetHub_BroadcastMethods_InvokeGroupSendAsync()
    {
        var mockGroups = new TestGroupManager();
        var mockClients = new TestHubCallerClients();
        var hub = new FleetHub(NullLogger<FleetHub>.Instance)
        {
            Context = new TestHubCallerContext("conn-broadcast"),
            Groups = mockGroups,
            Clients = mockClients
        };

        var tenantId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();

        // 1. Telemetry
        var telemetry = new VehicleTelemetryBroadcast(tenantId, vehicleId, "EV-01", 37.7749, -122.4194, 65.5m, 180m, 12500m, DateTime.UtcNow);
        await hub.SendVehicleTelemetry(telemetry);
        Assert.Equal("ReceiveVehicleTelemetry", mockClients.LastMethodInvoked);

        // 2. Battery
        var battery = new EvBatteryBroadcast(tenantId, vehicleId, 85.5m, 320m, 28.5m, EvChargingStatus.ChargingDcFast, 150m, DateTime.UtcNow);
        await hub.SendEvBatteryUpdate(battery);
        Assert.Equal("ReceiveEvBatteryTelemetry", mockClients.LastMethodInvoked);

        // 3. Charging session
        var session = new ChargingSessionBroadcast(tenantId, Guid.NewGuid(), vehicleId, Guid.NewGuid(), 90m, 45.2m, 14.50m, true);
        await hub.SendChargingSessionUpdate(session);
        Assert.Equal("ReceiveChargingSessionUpdate", mockClients.LastMethodInvoked);

        // 4. Route dispatch
        var dispatch = new RouteDispatchBroadcast(tenantId, Guid.NewGuid(), "RUN-2026-001", "Dispatched to driver", DateTime.UtcNow);
        await hub.SendRouteDispatchUpdate(dispatch);
        Assert.Equal("ReceiveRouteDispatchUpdate", mockClients.LastMethodInvoked);

        // 5. Predictive alert
        var alert = new PredictiveAlertBroadcast(tenantId, vehicleId, "Tesla Semi", "Inverter Power Stage", PredictiveRiskLevel.CriticalFailureImminent, 0.88m, 14);
        await hub.SendPredictiveAlert(alert);
        Assert.Equal("ReceivePredictiveAlert", mockClients.LastMethodInvoked);
    }

    // =========================================================================
    // TEST DOUBLES FOR SIGNALR HUB
    // =========================================================================

    private sealed class TestHubCallerContext : HubCallerContext
    {
        public TestHubCallerContext(string connectionId) => ConnectionId = connectionId;

        public override string ConnectionId { get; }
        public override string? UserIdentifier => "test-user";
        public override System.Security.Claims.ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override Microsoft.AspNetCore.Http.Features.IFeatureCollection Features { get; } = new Microsoft.AspNetCore.Http.Features.FeatureCollection();
        public override System.Threading.CancellationToken ConnectionAborted => default;
        public override void Abort() { }
    }

    private sealed class TestGroupManager : IGroupManager
    {
        public List<string> AddedGroups { get; } = new();
        public List<string> RemovedGroups { get; } = new();

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            AddedGroups.Add(groupName);
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            RemovedGroups.Add(groupName);
            return Task.CompletedTask;
        }
    }

    private sealed class TestHubCallerClients : IHubCallerClients
    {
        public string? LastMethodInvoked { get; set; }
        public object?[]? LastArgs { get; set; }

        public IClientProxy Group(string groupName) => new TestClientProxy(this);
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => new TestClientProxy(this);

        public IClientProxy All => new TestClientProxy(this);
        public IClientProxy Caller => new TestClientProxy(this);
        public IClientProxy Others => new TestClientProxy(this);
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => new TestClientProxy(this);
        public IClientProxy Client(string connectionId) => new TestClientProxy(this);
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => new TestClientProxy(this);
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => new TestClientProxy(this);
        public IClientProxy OthersInGroup(string groupName) => new TestClientProxy(this);
        public IClientProxy User(string userId) => new TestClientProxy(this);
        public IClientProxy Users(IReadOnlyList<string> userIds) => new TestClientProxy(this);
    }

    private sealed class TestClientProxy : IClientProxy
    {
        private readonly TestHubCallerClients _parent;

        public TestClientProxy(TestHubCallerClients parent)
        {
            _parent = parent;
        }

        public Task SendCoreAsync(string method, object?[]? args, CancellationToken cancellationToken = default)
        {
            _parent.LastMethodInvoked = method;
            _parent.LastArgs = args;
            return Task.CompletedTask;
        }
    }
}
