using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Compliance;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class ComplianceServiceTests
{
    private static (ConnectedOpsDbContext Db, TestUserContext Context, Guid TenantId, Guid UserId) CreateTestEnv()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "compliance.officer@connectedops.io"
        };
        var db = TestDbContextFactory.Create();
        return (db, context, tenantId, userId);
    }

    private static async Task<Vehicle> SeedVehicleAsync(ConnectedOpsDbContext db, Guid tenantId, string regNumber = "COMP-001")
    {
        var category = new VehicleCategory(tenantId, "Light Commercial", "LCV", "Commercial vans", true);
        var make = new VehicleMake(tenantId, "Toyota", "JP");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "HiAce", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(tenantId, "V-100", category.Id, make.Id, model.Id, registrationNumber: regNumber, vin: "VIN-TOYOTA-12345", modelYear: 2024);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return vehicle;
    }

    [Fact]
    public async Task CreateRequirement_ShouldPersist_AndRetrieveInPagedList()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var service = new ComplianceRequirementService(db, context, audit);

        var request = new CreateComplianceRequirementRequest(
            Code: "REQ-MOT-01",
            Name: "Annual Vehicle Safety Inspection",
            AppliesTo: ComplianceSubjectType.Vehicle,
            RequirementType: ComplianceRequirementType.Inspection,
            ValidityType: ComplianceValidityType.Recurring,
            Description: "Mandatory annual mechanical safety check",
            DefaultValidityDays: 365,
            DefaultReminderDays: 30,
            IsMandatory: true);

        var created = await service.CreateAsync(request);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("REQ-MOT-01", created.Code);
        Assert.Equal("Annual Vehicle Safety Inspection", created.Name);
        Assert.Equal(ComplianceSubjectType.Vehicle, created.AppliesTo);
        Assert.True(created.IsMandatory);

        var paged = await service.GetPagedAsync(new ComplianceRequirementFilterRequest(SearchTerm: "MOT"));
        Assert.Single(paged.Items);
        Assert.Equal("REQ-MOT-01", paged.Items.First().Code);
    }

    [Fact]
    public async Task CreateRecord_AndVerify_ShouldUpdateStatus()
    {
        var (db, context, tenantId, userId) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var expiryService = new ComplianceExpiryService();
        var reqService = new ComplianceRequirementService(db, context, audit);
        var recordService = new ComplianceRecordService(db, context, audit, expiryService);

        var vehicle = await SeedVehicleAsync(db, tenantId);

        var req = await reqService.CreateAsync(new CreateComplianceRequirementRequest(
            Code: "INS-001",
            Name: "Commercial Vehicle Insurance",
            AppliesTo: ComplianceSubjectType.Vehicle,
            RequirementType: ComplianceRequirementType.Insurance,
            DefaultValidityDays: 365));

        var createReq = new CreateComplianceRecordRequest(
            ComplianceRequirementId: req.Id,
            SubjectType: ComplianceSubjectType.Vehicle,
            VehicleId: vehicle.Id,
            ReferenceNumber: "POL-987654",
            IssueDateUtc: DateTime.UtcNow.Date.AddDays(-30),
            EffectiveFromUtc: DateTime.UtcNow.Date.AddDays(-30),
            ExpiryDateUtc: DateTime.UtcNow.Date.AddDays(335),
            Notes: "Fleet policy underwritten by Allianz");

        var record = await recordService.CreateAsync(createReq);

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal("POL-987654", record.ReferenceNumber);
        Assert.Null(record.VerifiedAtUtc);

        var verified = await recordService.VerifyAsync(record.Id, new VerifyComplianceRecordRequest(
            VerifiedByUserId: userId,
            Notes: "Certificate authenticity confirmed"));

        Assert.NotNull(verified.VerifiedAtUtc);
        Assert.Equal(userId, verified.VerifiedByUserId);
    }

    [Fact]
    public async Task ComplianceEvaluation_MissingMandatory_ReturnsMissingStatus()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var expiryService = new ComplianceExpiryService();
        var statusService = new ComplianceStatusService(expiryService);
        var scoreService = new ComplianceScoreService();
        var reqService = new ComplianceRequirementService(db, context, audit);
        var evalService = new ComplianceEvaluationService(db, context, statusService, expiryService, scoreService);

        var vehicle = await SeedVehicleAsync(db, tenantId);

        await reqService.CreateAsync(new CreateComplianceRequirementRequest(
            Code: "ROAD-TAX",
            Name: "Vehicle Road Tax",
            AppliesTo: ComplianceSubjectType.Vehicle,
            RequirementType: ComplianceRequirementType.Registration,
            IsMandatory: true));

        var eval = await evalService.EvaluateVehicleAsync(vehicle.Id);

        Assert.Equal(ComplianceStatus.Missing, eval.OverallStatus);
        Assert.Equal(1, eval.TotalRequirements);
        Assert.NotEmpty(eval.RequirementResults);
        Assert.Equal(ComplianceStatus.Missing, eval.RequirementResults.First().Status);
        Assert.True(eval.Score < 100);
    }

    [Fact]
    public async Task ComplianceException_ActiveException_MakesSubjectCompliant()
    {
        var (db, context, tenantId, userId) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var expiryService = new ComplianceExpiryService();
        var statusService = new ComplianceStatusService(expiryService);
        var scoreService = new ComplianceScoreService();
        var reqService = new ComplianceRequirementService(db, context, audit);
        var excService = new ComplianceExceptionService(db, context, audit);
        var evalService = new ComplianceEvaluationService(db, context, statusService, expiryService, scoreService);

        var vehicle = await SeedVehicleAsync(db, tenantId);

        var req = await reqService.CreateAsync(new CreateComplianceRequirementRequest(
            Code: "EMISS-TEST",
            Name: "Emissions Certificate",
            AppliesTo: ComplianceSubjectType.Vehicle,
            RequirementType: ComplianceRequirementType.Permit,
            IsMandatory: true));

        // Create approved exception
        var exc = await excService.CreateAsync(new CreateComplianceExceptionRequest(
            ComplianceRequirementId: req.Id,
            SubjectType: ComplianceSubjectType.Vehicle,
            Reason: "EV vehicle exempt from emissions testing",
            EffectiveFromUtc: DateTime.UtcNow.AddDays(-1),
            EffectiveToUtc: DateTime.UtcNow.AddDays(365),
            VehicleId: vehicle.Id,
            Status: ComplianceExceptionStatus.Approved));

        var eval = await evalService.EvaluateVehicleAsync(vehicle.Id);

        Assert.Equal(ComplianceStatus.Valid, eval.OverallStatus);
        Assert.True(eval.RequirementResults.First().HasActiveException);
    }

    [Fact]
    public async Task ComplianceException_Lifecycle_ApproveAndCancel()
    {
        var (db, context, tenantId, _) = CreateTestEnv();
        var audit = new TestAuditLogService();
        var reqService = new ComplianceRequirementService(db, context, audit);
        var excService = new ComplianceExceptionService(db, context, audit);

        var vehicle = await SeedVehicleAsync(db, tenantId);

        var req = await reqService.CreateAsync(new CreateComplianceRequirementRequest(
            Code: "EXC-TEST",
            Name: "Temporary Exemption Test",
            AppliesTo: ComplianceSubjectType.Vehicle,
            RequirementType: ComplianceRequirementType.Permit));

        var exc = await excService.CreateAsync(new CreateComplianceExceptionRequest(
            ComplianceRequirementId: req.Id,
            SubjectType: ComplianceSubjectType.Vehicle,
            Reason: "Parts backorder delayed inspection",
            EffectiveFromUtc: DateTime.UtcNow,
            EffectiveToUtc: DateTime.UtcNow.AddDays(30),
            VehicleId: vehicle.Id,
            Status: ComplianceExceptionStatus.Pending));

        Assert.Equal(ComplianceExceptionStatus.Pending, exc.Status);

        var approved = await excService.ApproveAsync(exc.Id, new ApproveComplianceExceptionRequest("Approved by Fleet Mgr"));
        Assert.Equal(ComplianceExceptionStatus.Approved, approved.Status);

        var cancelled = await excService.CancelAsync(exc.Id);
        Assert.Equal(ComplianceExceptionStatus.Cancelled, cancelled.Status);
    }
}
