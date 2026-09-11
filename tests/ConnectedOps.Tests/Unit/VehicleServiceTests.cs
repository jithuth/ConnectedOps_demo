using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Vehicles;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class VehicleServiceTests
{
    private static async Task<(Guid CategoryId, Guid MakeId, Guid ModelId)> SeedPrerequisitesAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId)
    {
        var category = new VehicleCategory(tenantId, "Light Commercial Vehicle", "LCV", "Vans and small utility trucks", true);
        var make = new VehicleMake(tenantId, "Ford", "US");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Transit 350", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        return (category.Id, make.Id, model.Id);
    }

    [Fact]
    public async Task CreateVehicleAsync_ValidRequest_CreatesVehicleAndInitialOdometerEntry()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = userId, Email = "fleet@connectedops.io" };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var request = new CreateVehicleRequest(
            VehicleNumber: "FLT-001",
            CategoryId: catId,
            MakeId: makeId,
            ModelId: modelId,
            DisplayName: "Transit Van 1",
            InternalCode: "VAN-01",
            RegistrationNumber: "ABC-1234",
            VIN: "1FTNE3Y89KDA12345",
            ChassisNumber: "CHS-9988",
            EngineNumber: "ENG-4455",
            ModelYear: 2024,
            ManufactureYear: 2024,
            FuelType: FuelType.Diesel,
            TransmissionType: TransmissionType.Automatic,
            OwnershipType: OwnershipType.CompanyOwned,
            BranchId: null,
            LocationId: null,
            InitialOdometer: 1500m,
            OdometerUnit: OdometerUnit.Kilometers,
            Status: VehicleStatus.Active,
            Color: "Oxford White",
            NumberOfSeats: 3,
            GrossVehicleWeight: 3500m,
            PayloadCapacity: 1200m,
            OwnerName: "Acme Logistics",
            LeaseCompany: null,
            LeaseStartDate: null,
            LeaseEndDate: null,
            MonthlyLeaseCost: null,
            PurchaseDate: new DateOnly(2024, 1, 15),
            PurchasePrice: 45000m,
            CurrencyCode: "USD",
            InServiceDate: new DateOnly(2024, 2, 1),
            Notes: "Assigned to regional deliveries");

        var created = await vehicleService.CreateVehicleAsync(request);

        Assert.NotNull(created);
        Assert.Equal("FLT-001", created.VehicleNumber);
        Assert.Equal("Transit Van 1", created.DisplayName);
        Assert.Equal(1500m, created.CurrentOdometer);
        Assert.Equal(VehicleStatus.Active, created.Status);
        Assert.Equal("Ford", created.MakeName);
        Assert.Equal("Transit 350", created.ModelName);
        Assert.Equal("Light Commercial Vehicle", created.CategoryName);

        // Verify initial odometer entry created
        Assert.Single(created.RecentOdometerEntries);
        Assert.Equal(1500m, created.RecentOdometerEntries.First().Reading);

        // Verify audit log recorded
        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.Created && l.EntityType == "Vehicle");
    }

    [Fact]
    public async Task CreateVehicleAsync_DuplicateVehicleNumber_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid(), Email = "admin@ops.io" };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var req1 = new CreateVehicleRequest(
            "FLT-100", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        await vehicleService.CreateVehicleAsync(req1);

        var req2 = new CreateVehicleRequest(
            "flt-100", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<ConflictException>(() => vehicleService.CreateVehicleAsync(req2));
    }

    [Fact]
    public async Task CreateVehicleAsync_NonExistentCategory_ThrowsKeyNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (_, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var request = new CreateVehicleRequest(
            "FLT-200", Guid.NewGuid(), makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => vehicleService.CreateVehicleAsync(request));
    }

    [Fact]
    public async Task GetVehiclesPagedAsync_EnforcesStrictTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (catA, makeA, modelA) = await SeedPrerequisitesAsync(db, tenantA);
        var (catB, makeB, modelB) = await SeedPrerequisitesAsync(db, tenantB);

        var ctxA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var ctxB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var serviceA = new VehicleService(db, ctxA, audit);
        var serviceB = new VehicleService(db, ctxB, audit);

        await serviceA.CreateVehicleAsync(new CreateVehicleRequest(
            "TENANT-A-VEH", catA, makeA, modelA, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 100m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        await serviceB.CreateVehicleAsync(new CreateVehicleRequest(
            "TENANT-B-VEH", catB, makeB, modelB, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 200m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        var resultA = await serviceA.GetVehiclesPagedAsync(new VehicleQueryParameters());
        var resultB = await serviceB.GetVehiclesPagedAsync(new VehicleQueryParameters());

        Assert.Single(resultA.Items);
        Assert.Equal("TENANT-A-VEH", resultA.Items.First().VehicleNumber);

        Assert.Single(resultB.Items);
        Assert.Equal("TENANT-B-VEH", resultB.Items.First().VehicleNumber);
    }

    [Fact]
    public async Task GetVehicleByIdAsync_OtherTenantVehicle_ThrowsKeyNotFoundException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var (catA, makeA, modelA) = await SeedPrerequisitesAsync(db, tenantA);
        var ctxA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var ctxB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var serviceA = new VehicleService(db, ctxA, audit);
        var serviceB = new VehicleService(db, ctxB, audit);

        var vehA = await serviceA.CreateVehicleAsync(new CreateVehicleRequest(
            "PRIV-01", catA, makeA, modelA, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.GetVehicleByIdAsync(vehA.Id));
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidTransitions_UpdatesStatusAndAuditNote()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid(), Email = "fleet@ops.io" };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var veh = await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "STATUS-TEST", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        // Transition: Active -> InService
        var updated = await vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.InService, "Deployed to route 42"));
        Assert.Equal(VehicleStatus.InService, updated.Status);

        // Transition: InService -> UnderMaintenance
        updated = await vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.UnderMaintenance, "Scheduled 50k service"));
        Assert.Equal(VehicleStatus.UnderMaintenance, updated.Status);

        // Transition: UnderMaintenance -> InService
        updated = await vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.InService, "Maintenance complete"));
        Assert.Equal(VehicleStatus.InService, updated.Status);

        // Check notes list recorded the audit transitions
        Assert.Equal(3, updated.NotesList.Count);
        Assert.Contains(updated.NotesList, n => n.NoteText.Contains("Deployed to route 42"));
    }

    [Fact]
    public async Task ChangeStatusAsync_TerminalState_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var veh = await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "TERMINAL-TEST", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        // Retire vehicle (terminal status)
        await vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.Retired, "Vehicle decommissioned"));

        // Attempting to move from Retired back to Active must fail
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.Active, "Reactivate")));
    }

    [Fact]
    public async Task ChangeStatusAsync_InvalidTransition_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var veh = await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "INVALID-TRANS", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Draft,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        // Draft cannot transition directly to UnderMaintenance
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            vehicleService.ChangeStatusAsync(veh.Id, new ChangeVehicleStatusRequest(VehicleStatus.UnderMaintenance, "Direct to maintenance")));
    }

    [Fact]
    public async Task RecordOdometerAsync_ValidReading_UpdatesVehicleOdometerAndHistory()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);
        var odoService = new VehicleOdometerService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var veh = await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "ODO-TEST", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 1000m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        var req = new RecordVehicleOdometerRequest(1250.5m, OdometerUnit.Kilometers, DateTime.UtcNow, OdometerSource.Manual, "Weekly report");
        var entry = await odoService.RecordOdometerAsync(veh.Id, req);

        Assert.NotNull(entry);
        Assert.Equal(1250.5m, entry.Reading);

        // Verify vehicle current odometer updated
        var updatedVeh = await vehicleService.GetVehicleByIdAsync(veh.Id);
        Assert.Equal(1250.5m, updatedVeh.CurrentOdometer);
    }

    [Fact]
    public async Task RecordOdometerAsync_LowerThanCurrent_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var vehicleService = new VehicleService(db, userContext, auditService);
        var odoService = new VehicleOdometerService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        var veh = await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "ODO-LOWER", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 5000m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        var req = new RecordVehicleOdometerRequest(4800m, OdometerUnit.Kilometers, DateTime.UtcNow, OdometerSource.Manual, "Tampered reading");

        await Assert.ThrowsAsync<InvalidOperationException>(() => odoService.RecordOdometerAsync(veh.Id, req));
    }

    [Fact]
    public void VehicleDocument_ExpiryCalculation_WorksCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expiredDoc = new VehicleDocument(
            tenantId, vehicleId, VehicleDocumentType.Insurance, "Policy 2023",
            "INS-001", today.AddYears(-2), today.AddDays(-5), "Allianz",
            "keys/ins1.pdf", "ins1.pdf", "application/pdf", 2048, null);

        var expiringSoonDoc = new VehicleDocument(
            tenantId, vehicleId, VehicleDocumentType.Registration, "Registration",
            "REG-001", today.AddYears(-1), today.AddDays(15), "DMV",
            "keys/reg.pdf", "reg.pdf", "application/pdf", 1024, null);

        var validDoc = new VehicleDocument(
            tenantId, vehicleId, VehicleDocumentType.RoadPermit, "Hazmat Permit",
            "HAZ-99", today, today.AddDays(180), "DOT",
            "keys/permit.pdf", "permit.pdf", "application/pdf", 4096, null);

        Assert.True(expiredDoc.IsExpired());
        Assert.False(expiredDoc.IsExpiringSoon()); // Already expired, not "expiring soon"

        Assert.False(expiringSoonDoc.IsExpired());
        Assert.True(expiringSoonDoc.IsExpiringSoon());

        Assert.False(validDoc.IsExpired());
        Assert.False(validDoc.IsExpiringSoon());
    }

    [Fact]
    public async Task CreateCategoryAsync_DuplicateCode_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var catService = new VehicleCategoryService(db, userContext, auditService);

        await catService.CreateCategoryAsync(new CreateVehicleCategoryRequest("Trucks", "TRK", "Heavy Trucks", true));

        await Assert.ThrowsAsync<ConflictException>(() =>
            catService.CreateCategoryAsync(new CreateVehicleCategoryRequest("Different Name Same Code", "trk", null, true)));
    }

    [Fact]
    public async Task DeleteCategoryAsync_WithAssociatedVehicles_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var catService = new VehicleCategoryService(db, userContext, auditService);
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "CAT-DEL-TEST", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        await Assert.ThrowsAsync<ConflictException>(() => catService.DeleteCategoryAsync(catId));
    }

    [Fact]
    public async Task DeleteMakeAsync_WithModelsOrVehicles_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var makeModelService = new VehicleMakeModelService(db, userContext, auditService);

        var (_, makeId, _) = await SeedPrerequisitesAsync(db, tenantId);

        // Make has model Transit 350 linked, so DeleteMakeAsync must throw ConflictException
        await Assert.ThrowsAsync<ConflictException>(() => makeModelService.DeleteMakeAsync(makeId));
    }

    [Fact]
    public async Task DeleteModelAsync_WithVehicles_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var makeModelService = new VehicleMakeModelService(db, userContext, auditService);
        var vehicleService = new VehicleService(db, userContext, auditService);

        var (catId, makeId, modelId) = await SeedPrerequisitesAsync(db, tenantId);

        await vehicleService.CreateVehicleAsync(new CreateVehicleRequest(
            "MOD-DEL-TEST", catId, makeId, modelId, null, null, null, null, null, null,
            null, null, FuelType.Diesel, TransmissionType.Automatic, OwnershipType.CompanyOwned,
            null, null, 0m, OdometerUnit.Kilometers, VehicleStatus.Active,
            null, null, null, null, null, null, null, null, null, null, null, null, null, null));

        await Assert.ThrowsAsync<ConflictException>(() => makeModelService.DeleteModelAsync(modelId));
    }
}
