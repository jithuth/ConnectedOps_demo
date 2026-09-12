using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Safety;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class SafetyServiceTests
{
    private static (ConnectedOpsDbContext Db, TestUserContext Context, Guid TenantId, Guid UserId) CreateTestEnv()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "safety.manager@connectedops.io"
        };
        var db = TestDbContextFactory.Create();
        return (db, context, tenantId, userId);
    }

    private static async Task<(Vehicle Vehicle, Driver Driver, Employee Employee)> SeedEntitiesAsync(ConnectedOpsDbContext db, Guid tenantId)
    {
        var category = new VehicleCategory(tenantId, "Heavy Truck", "HT", "Heavy commercial vehicles", true);
        var make = new VehicleMake(tenantId, "Volvo", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "FH16", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, "TRK-001", category.Id, make.Id, model.Id, registrationNumber: "REG-SAFETY-1", vin: "VIN-VOLVO-9999", modelYear: 2025);
        db.Vehicles.Add(vehicle);

        var employee = new Employee(tenantId, "Officer", "Safety", "officer.safety@connectedops.io", "555-0199", "EMP-900");
        db.Employees.Add(employee);

        var driver = new Driver(tenantId, "DRV-100", "Bob", "Driver", "555-0200", DriverType.Employee);
        db.Drivers.Add(driver);

        await db.SaveChangesAsync();

        return (vehicle, driver, employee);
    }

    [Fact]
    public async Task CreateIncident_GeneratesIncidentNumber_AndPersistsSuccessfully()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var numGen = new IncidentNumberGenerator(db);
        var service = new SafetyIncidentService(db, context, audit, numGen);

        var (vehicle, driver, _) = await SeedEntitiesAsync(db, tenantId);

        var request = new CreateSafetyIncidentRequest(
            IncidentType: SafetyIncidentType.VehicleAccident,
            Severity: SafetyIncidentSeverity.Moderate,
            OccurredAtUtc: DateTime.UtcNow.AddHours(-2),
            Title: "Low-speed loading dock bump",
            Description: "Rear bumper backed into loading bay rubber stop",
            PrimaryVehicleId: vehicle.Id,
            PrimaryDriverId: driver.Id,
            InvestigationRequired: true);

        var created = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.StartsWith($"INC-{DateTime.UtcNow.Year}-", created.IncidentNumber);
        Assert.Equal("Low-speed loading dock bump", created.Title);
        Assert.Equal(SafetyIncidentType.VehicleAccident, created.IncidentType);
        Assert.Equal(SafetyIncidentSeverity.Moderate, created.Severity);
        Assert.Equal(SafetyIncidentStatus.Open, created.Status);

        var fetched = await service.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(created.IncidentNumber, fetched.IncidentNumber);
        Assert.True(fetched.InvestigationRequired);
    }

    [Fact]
    public async Task SafetyIncident_InvestigationWorkflow_Lifecycle()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var numGen = new IncidentNumberGenerator(db);
        var service = new SafetyIncidentService(db, context, audit, numGen);

        var (vehicle, driver, employee) = await SeedEntitiesAsync(db, tenantId);

        var created = await service.CreateAsync(new CreateSafetyIncidentRequest(
            IncidentType: SafetyIncidentType.NearMiss,
            Severity: SafetyIncidentSeverity.High,
            OccurredAtUtc: DateTime.UtcNow.AddDays(-1),
            Title: "Near miss at rail crossing",
            Description: "Crossing barrier lowered while crossing intersection",
            PrimaryVehicleId: vehicle.Id,
            PrimaryDriverId: driver.Id,
            InvestigationRequired: true));

        // Start investigation
        var invStarted = await service.StartInvestigationAsync(created.Id, new StartSafetyInvestigationRequest(
            InvestigatorEmployeeId: employee.Id));

        Assert.Equal(SafetyIncidentStatus.UnderInvestigation, invStarted.Status);
        Assert.Equal(SafetyInvestigationStatus.InProgress, invStarted.InvestigationStatus);

        // Complete investigation
        var invCompleted = await service.CompleteInvestigationAsync(created.Id, new CompleteSafetyInvestigationRequest(
            Summary: "Signals actuated prematurely without sufficient amber clearance interval",
            RootCause: SafetyRootCause.ProcedureFailure,
            ContributingFactors: "Weather: Rain",
            Recommendation: "Re-calibrate timing of sensor loop; brief drivers on approach speeds"));

        Assert.Equal(SafetyInvestigationStatus.Completed, invCompleted.InvestigationStatus);

        // Check full details via GetByIdAsync
        var details = await service.GetByIdAsync(created.Id);
        Assert.NotNull(details);
        Assert.NotNull(details.Investigation);
        Assert.Equal(SafetyRootCause.ProcedureFailure, details.Investigation.RootCause);

        // Close incident
        var closed = await service.CloseAsync(created.Id, new CloseSafetyIncidentRequest(
            Notes: "Investigation concluded. Remedial notice issued."));

        Assert.Equal(SafetyIncidentStatus.Closed, closed.Status);
        Assert.NotNull(closed.ClosedAtUtc);
    }

    [Fact]
    public async Task SafetyViolation_CreateAndResolve_Lifecycle()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var service = new SafetyViolationService(db, context, audit);

        var (vehicle, driver, _) = await SeedEntitiesAsync(db, tenantId);

        var violation = await service.CreateAsync(new CreateSafetyViolationRequest(
            ViolationType: SafetyViolationType.Speeding,
            Severity: SafetyIncidentSeverity.Moderate,
            Source: SafetyViolationSource.Telematics,
            OccurredAtUtc: DateTime.UtcNow.AddHours(-1),
            Description: "Exceeded 80 km/h in 60 km/h commercial depot zone",
            DriverId: driver.Id,
            VehicleId: vehicle.Id,
            Reference: "TEL-EVT-9988"));

        Assert.NotEqual(Guid.Empty, violation.Id);
        Assert.False(violation.IsResolved);
        Assert.Equal("TEL-EVT-9988", violation.Reference);

        var resolved = await service.ResolveAsync(violation.Id, new ResolveSafetyViolationRequest(
            ResolutionNotes: "Driver coached on yard speed rules. Telematics warning acknowledged."));

        Assert.True(resolved.IsResolved);
        Assert.NotNull(resolved.ResolvedAtUtc);
        Assert.Equal("Driver coached on yard speed rules. Telematics warning acknowledged.", resolved.ResolutionNotes);
    }

    [Fact]
    public async Task CorrectiveAction_FullRemediationLifecycle()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var service = new CorrectiveActionService(db, context, audit);

        var (_, _, employee) = await SeedEntitiesAsync(db, tenantId);

        var action = await service.CreateAsync(new CreateCorrectiveActionRequest(
            Title: "Install auxiliary blind-spot proximity mirrors",
            Description: "Retrofit dual wide-angle mirrors on right-hand side of heavy truck",
            Priority: CorrectiveActionPriority.High,
            DueDateUtc: DateTime.UtcNow.AddDays(14),
            AssignedEmployeeId: employee.Id,
            VerificationRequired: true));

        Assert.Equal(CorrectiveActionStatus.Open, action.Status);
        Assert.True(action.VerificationRequired);

        // Start
        var started = await service.StartAsync(action.Id);
        Assert.Equal(CorrectiveActionStatus.InProgress, started.Status);

        // Complete
        var completed = await service.CompleteAsync(action.Id, new CompleteCorrectiveActionRequest(
            ResolutionNotes: "Mirrors fitted and calibrated in workshop bay 4"));
        Assert.Equal(CorrectiveActionStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAtUtc);

        // Verify
        var verified = await service.VerifyAsync(action.Id, new VerifyCorrectiveActionRequest(
            VerificationNotes: "Visual workshop inspection confirmed correct adjustment"));
        Assert.Equal(CorrectiveActionStatus.Verified, verified.Status);
        Assert.NotNull(verified.VerifiedAtUtc);
    }

    [Fact]
    public async Task SafetyDashboard_AggregatesMetricsAccurately()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var numGen = new IncidentNumberGenerator(db);
        var incidentService = new SafetyIncidentService(db, context, audit, numGen);
        var scoreService = new SafetyScoreService(db, context);
        var dashboardService = new SafetyDashboardService(db, context, scoreService);

        var (vehicle, driver, _) = await SeedEntitiesAsync(db, tenantId);

        await incidentService.CreateAsync(new CreateSafetyIncidentRequest(
            IncidentType: SafetyIncidentType.NearMiss,
            Severity: SafetyIncidentSeverity.Low,
            OccurredAtUtc: DateTime.UtcNow,
            Title: "Minor lane deviation",
            Description: "Lane drift warning triggered on highway",
            PrimaryVehicleId: vehicle.Id,
            PrimaryDriverId: driver.Id));

        await incidentService.CreateAsync(new CreateSafetyIncidentRequest(
            IncidentType: SafetyIncidentType.VehicleAccident,
            Severity: SafetyIncidentSeverity.Critical,
            OccurredAtUtc: DateTime.UtcNow,
            Title: "Tire blowout on highway",
            Description: "Right steer tire separated at 90 km/h",
            PrimaryVehicleId: vehicle.Id,
            PrimaryDriverId: driver.Id));

        var dashboard = await dashboardService.GetDashboardMetricsAsync();

        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.IncidentsThisMonth);
        Assert.Equal(2, dashboard.OpenIncidents);
        Assert.Equal(1, dashboard.CriticalIncidents);
        Assert.NotEmpty(dashboard.RecentIncidents);
        Assert.True(dashboard.SafetyScore >= 0 && dashboard.SafetyScore <= 100);
    }
}
