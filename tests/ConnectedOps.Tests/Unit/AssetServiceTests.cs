using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Assets;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class AssetServiceTests
{
    private static (ConnectedOpsDbContext Db, TestUserContext Context, Guid TenantId, Guid UserId) CreateTestEnv()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var context = new TestUserContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = "tester@connectedops.io"
        };
        var db = TestDbContextFactory.Create();
        return (db, context, tenantId, userId);
    }

    private static async Task<(AssetCategory Category, AssetType AssetType)> SeedCategoryAndTypeAsync(ConnectedOpsDbContext db, Guid tenantId)
    {
        var category = new AssetCategory(tenantId, "PWR-TOOLS", "Power Tools", "Handheld electric & battery equipment");
        db.AssetCategories.Add(category);
        await db.SaveChangesAsync();

        var type = new AssetType(tenantId, category.Id, "ROT-HAMMER", "Rotary Hammer Drill", "Heavy duty rotary hammer", requiresSerialNumber: true, requiresInspection: true, requiresCalibration: true, requiresWarrantyTracking: true);
        db.AssetTypes.Add(type);
        await db.SaveChangesAsync();

        return (category, type);
    }

    private static async Task<Employee> SeedEmployeeAsync(ConnectedOpsDbContext db, Guid tenantId, string empNumber = "EMP-101")
    {
        var emp = new Employee(tenantId, "John", "Doe", $"{empNumber.ToLower()}@connectedops.io", "555-0100", empNumber);
        db.Employees.Add(emp);
        await db.SaveChangesAsync();
        return emp;
    }

    private static async Task<Vehicle> SeedVehicleAsync(ConnectedOpsDbContext db, Guid tenantId, string regNumber = "TRK-900")
    {
        var category = new VehicleCategory(tenantId, "Work Van", "WV", "Service Vans", true);
        var make = new VehicleMake(tenantId, "Ford", "US");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Transit 350", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId: tenantId,
            vehicleNumber: "VAN-01",
            categoryId: category.Id,
            makeId: make.Id,
            modelId: model.Id,
            displayName: "Ford Transit #01",
            internalCode: "VN-01",
            registrationNumber: regNumber,
            vin: $"VIN{Guid.NewGuid():N}"[..17].ToUpperInvariant(),
            chassisNumber: "CHS-01",
            engineNumber: "ENG-01",
            modelYear: 2024,
            manufactureYear: 2024,
            fuelType: FuelType.Diesel,
            transmissionType: TransmissionType.Automatic,
            ownershipType: OwnershipType.CompanyOwned,
            branchId: null,
            locationId: null,
            currentOdometer: 10000m,
            odometerUnit: OdometerUnit.Kilometers,
            status: VehicleStatus.InService);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    [Fact]
    public async Task CategoryAndTypeService_CanCreateAndRetrieveTree()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var catService = new AssetCategoryService(db, context, auditLog);
        var typeService = new AssetTypeService(db, context, auditLog);

        // Act - Create parent category
        var parentCat = await catService.CreateAsync(new CreateAssetCategoryRequest("HEAVY-EQ", "Heavy Equipment", "Civil & site machinery", null));
        // Create sub category
        var subCat = await catService.CreateAsync(new CreateAssetCategoryRequest("EXCAVATORS", "Excavators", "Mini & full-size excavators", parentCat.Id));

        // Create Asset Types
        var type1 = await typeService.CreateAsync(new CreateAssetTypeRequest(subCat.Id, "MINI-EXC", "Mini Excavator 3T", "3-ton rubber track", true, 60, false, null, true));

        var tree = await catService.GetTreeAsync();
        var allTypes = await typeService.GetAllAsync(subCat.Id);

        // Assert
        Assert.Single(tree);
        Assert.Equal("Heavy Equipment", tree[0].Name);
        Assert.Single(tree[0].Children);
        Assert.Equal("Excavators", tree[0].Children[0].Name);
        Assert.Single(allTypes);
        Assert.Equal("Mini Excavator 3T", allTypes[0].Name);
    }

    [Fact]
    public async Task AssetService_CRUD_Lifecycle_WorksCorrectly()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var assetService = new AssetService(db, context, auditLog);

        // 1. Create Asset
        var createReq = new CreateAssetRequest(
            AssetNumber: "AST-HAM-001",
            Name: "DeWalt SDS Max Rotary Hammer",
            Description: "Heavy duty concrete breaker",
            SerialNumber: "DW-987654321",
            InternalCode: "TOOL-001",
            Barcode: "885911223344",
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "DeWalt",
            Model: "D25762K",
            Year: "2024",
            OwnershipType: AssetOwnershipType.CompanyOwned,
            Condition: AssetCondition.Excellent,
            PurchaseCost: 899.99m,
            PurchaseDate: DateTime.UtcNow.AddMonths(-3),
            VendorName: "Industrial Tool Supply Co.",
            WarrantyExpiryDate: DateTime.UtcNow.AddMonths(21),
            ExpectedLifespanMonths: 60,
            IsCritical: true);

        var created = await assetService.CreateAsync(createReq);
        Assert.NotNull(created);
        Assert.Equal("AST-HAM-001", created.AssetNumber);
        Assert.Equal(AssetStatus.Available, created.Status);

        // 2. Get Details
        var details = await assetService.GetByIdAsync(created.Id);
        Assert.NotNull(details);
        Assert.Equal("DeWalt", details.Make);

        // 3. Update Asset
        var updateReq = new UpdateAssetRequest(
            Name: "DeWalt SDS Max Rotary Hammer - Upgraded",
            Description: "Heavy duty concrete breaker with anti-vibration",
            SerialNumber: details.SerialNumber,
            InternalCode: details.InternalCode,
            Barcode: details.Barcode,
            RfidTag: "RFID-TAG-99",
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: details.Make,
            Model: details.Model,
            Year: details.Year,
            OwnershipType: details.OwnershipType,
            DepartmentId: null,
            TeamId: null,
            PurchaseCost: details.PurchaseCost,
            PurchaseDate: details.PurchaseDate,
            VendorName: details.VendorName,
            WarrantyExpiryDate: details.WarrantyExpiryDate,
            WarrantyNotes: "Extended warranty active",
            ExpectedLifespanMonths: details.ExpectedLifespanMonths,
            IsCritical: details.IsCritical,
            IsActive: true);

        var updated = await assetService.UpdateAsync(created.Id, updateReq);
        Assert.Equal("DeWalt SDS Max Rotary Hammer - Upgraded", updated.Name);

        // 4. Change Status
        var statusResult = await assetService.ChangeStatusAsync(created.Id, new ChangeAssetStatusRequest(AssetStatus.UnderMaintenance, "Scheduled preventative service"));
        Assert.Equal(AssetStatus.UnderMaintenance, statusResult.Status);

        // 5. Add Note
        var note = await assetService.AddNoteAsync(created.Id, new AddAssetNoteRequest("Replaced carbon brushes and tested chuck alignment."));
        Assert.NotNull(note);
        Assert.Contains("carbon brushes", note.Note);

        var notes = await assetService.GetNotesAsync(created.Id);
        Assert.Equal(2, notes.Count);
        Assert.Contains(notes, n => n.Note.Contains("carbon brushes"));
    }

    [Fact]
    public async Task CustodyService_EmployeeAssignmentAndUsageSessions_WorkCorrectly()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var emp = await SeedEmployeeAsync(db, tenantId, "EMP-200");
        var assetService = new AssetService(db, context, auditLog);
        var custodyService = new AssetCustodyService(db, context, auditLog);

        var asset = await assetService.CreateAsync(new CreateAssetRequest(
            AssetNumber: "AST-GEN-02",
            Name: "Honda EU3000iS Generator",
            Description: "Silent inverter generator",
            SerialNumber: "EZFG-123456",
            InternalCode: null,
            Barcode: null,
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "Honda",
            Model: "EU3000iS",
            Year: "2023"));

        // Act 1: Employee Custody Assignment
        var assignment = await custodyService.AssignToEmployeeAsync(new AssignAssetToEmployeeRequest(asset.Id, emp.Id, AssetCondition.Good, DateTime.UtcNow.AddMonths(6), "Primary field unit"));
        Assert.NotNull(assignment);
        Assert.True(assignment.IsActive);

        var assignedAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Equal(emp.Id, assignedAsset!.CurrentCustodianEmployeeId);

        // Act 2: Checkout Usage Session
        var checkoutReq = new StartAssetUsageSessionRequest(
            AssetId: asset.Id,
            EmployeeId: emp.Id,
            VehicleId: null,
            MeterHours: 120.5m,
            Condition: AssetCondition.Good,
            ExpectedReturnUtc: DateTime.UtcNow.AddDays(2),
            Purpose: "Emergency field lighting power",
            Notes: "Fresh oil fill");

        var session = await custodyService.CheckOutAsync(checkoutReq);
        Assert.NotNull(session);
        Assert.Equal(AssetUsageSessionStatus.Open, session.Status);

        var inUseAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Equal(AssetStatus.InUse, inUseAsset!.Status);

        // Act 3: Check-in Session
        var checkinReq = new EndAssetUsageSessionRequest(
            MeterHours: 135.0m, // 14.5 hrs used
            Condition: AssetCondition.Good,
            Notes: "Returned in good working order");

        var returnedSession = await custodyService.CheckInAsync(session.Id, checkinReq);
        Assert.Equal(AssetUsageSessionStatus.Completed, returnedSession.Status);
        Assert.NotNull(returnedSession.CheckedInAtUtc);

        var availAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Equal(AssetStatus.Available, availAsset!.Status);

        // Act 4: Return Employee Custody
        var returnedCustody = await custodyService.ReturnFromEmployeeAsync(assignment.Id, new ReturnAssetFromEmployeeRequest(AssetCondition.Good, "Depot check-in"));
        Assert.False(returnedCustody.IsActive);

        var unassignedAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Null(unassignedAsset!.CurrentCustodianEmployeeId);
    }

    [Fact]
    public async Task CustodyService_VehicleMounting_WorksCorrectly()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var vehicle = await SeedVehicleAsync(db, tenantId, "SVC-555");
        var assetService = new AssetService(db, context, auditLog);
        var custodyService = new AssetCustodyService(db, context, auditLog);

        var asset = await assetService.CreateAsync(new CreateAssetRequest(
            AssetNumber: "AST-COMP-01",
            Name: "Van-Mounted Air Compressor",
            Description: "High pressure mobile compressor",
            SerialNumber: "CMP-9001",
            InternalCode: null,
            Barcode: null,
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "Ingersoll Rand",
            Model: "Type 30",
            Year: "2024"));

        // Act: Assign / Mount to Vehicle
        var assign = await custodyService.AssignToVehicleAsync(new AssignAssetToVehicleRequest(asset.Id, vehicle.Id, "Bolted to cargo bulkhead"));
        Assert.NotNull(assign);
        Assert.True(assign.IsActive);

        var mountedAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Equal(vehicle.Id, mountedAsset!.CurrentAssignedVehicleId);
        Assert.Equal(AssetAssignmentType.Permanent, mountedAsset.AssignmentType);

        // Act: Remove from Vehicle
        var unmount = await custodyService.RemoveFromVehicleAsync(assign.Id, new RemoveAssetFromVehicleRequest("Removed for overhaul"));
        Assert.False(unmount.IsActive);

        var unmountedAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Null(unmountedAsset!.CurrentAssignedVehicleId);
    }

    [Fact]
    public async Task AssetTransferService_TransferWorkflow_ExecutesCorrectly()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var emp1 = await SeedEmployeeAsync(db, tenantId, "EMP-ORIGIN");
        var emp2 = await SeedEmployeeAsync(db, tenantId, "EMP-DEST");
        var assetService = new AssetService(db, context, auditLog);
        var transferService = new AssetTransferService(db, context, auditLog);

        var asset = await assetService.CreateAsync(new CreateAssetRequest(
            AssetNumber: "AST-PUMP-01",
            Name: "Submersible Trash Pump 3-Inch",
            Description: "Dewatering pump",
            SerialNumber: "PMP-8877",
            InternalCode: null,
            Barcode: null,
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "Wacker Neuson",
            Model: "PTS 4V",
            Year: "2024"));

        // 1. Initiate Transfer
        var transfer = await transferService.CreateTransferAsync(new CreateAssetTransferRequest(
            AssetId: asset.Id,
            TransferType: AssetTransferType.Employee,
            TargetEmployeeId: emp2.Id,
            Reason: "Handover for weekend nightshift operations"));

        Assert.Equal(AssetTransferStatus.Pending, transfer.Status);

        // 2. Complete Transfer
        var completed = await transferService.CompleteTransferAsync(transfer.Id, new CompleteAssetTransferRequest("Received in good condition"));
        Assert.Equal(AssetTransferStatus.Completed, completed.Status);
        Assert.NotNull(completed.CompletedAtUtc);

        // Verify asset custodian updated to emp2
        var transferredAsset = await assetService.GetByIdAsync(asset.Id);
        Assert.Equal(emp2.Id, transferredAsset!.CurrentCustodianEmployeeId);
    }

    [Fact]
    public async Task AssetInspectionService_SafetyInspectionAndCalibration_Passes()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var emp = await SeedEmployeeAsync(db, tenantId, "EMP-QA-LEAD");
        var assetService = new AssetService(db, context, auditLog);
        var inspectionService = new AssetInspectionService(db, context, auditLog);

        var asset = await assetService.CreateAsync(new CreateAssetRequest(
            AssetNumber: "AST-METER-01",
            Name: "Fluke 87V Industrial Multimeter",
            Description: "True-RMS multimeter",
            SerialNumber: "FLK-445566",
            InternalCode: null,
            Barcode: null,
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "Fluke",
            Model: "87V",
            Year: "2024"));

        // 1. Perform Safety Inspection with Checklist Items
        var inspectionReq = new CreateAssetInspectionRequest(
            AssetId: asset.Id,
            InspectionType: AssetInspectionType.Safety,
            InspectionDateUtc: DateTime.UtcNow,
            InspectorEmployeeId: emp.Id,
            Result: AssetInspectionResult.Passed,
            Remarks: "Instrument passed all high voltage safety and insulation checks",
            NextInspectionDueUtc: DateTime.UtcNow.AddDays(90),
            Items:
            [
                new CreateAssetInspectionItemRequest("Test leads physical integrity", true),
                new CreateAssetInspectionItemRequest("Fused terminal protection", true),
                new CreateAssetInspectionItemRequest("Display backlight & contrast", true),
                new CreateAssetInspectionItemRequest("Battery compartment seal", true)
            ]);

        var insp = await inspectionService.CreateInspectionAsync(inspectionReq);
        Assert.Equal(AssetInspectionResult.Passed, insp.Result);
        Assert.Equal(4, insp.Items.Count);

        // 2. Perform Lab Calibration Record
        var calReq = new CreateAssetCalibrationRecordRequest(
            AssetId: asset.Id,
            CalibrationDateUtc: DateTime.UtcNow,
            PerformedBy: "Trescal ISO-17025 Metrology Lab",
            CertificateNumber: "CERT-NIST-2026-9912",
            NextCalibrationDueUtc: DateTime.UtcNow.AddDays(365),
            IsPassed: true,
            Remarks: "Within +/- 0.05% tolerance across all DC/AC ranges");

        var cal = await inspectionService.AddCalibrationRecordAsync(calReq);
        Assert.True(cal.IsPassed);
        Assert.Equal("CERT-NIST-2026-9912", cal.CertificateNumber);
    }

    [Fact]
    public async Task AssetIdentifierAndQrService_SecureScanToken_ResolvesAsset()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var assetService = new AssetService(db, context, auditLog);
        var identifierService = new AssetIdentifierService(db, context, auditLog);
        var qrService = new AssetQrCodeService();

        var asset = await assetService.CreateAsync(new CreateAssetRequest(
            AssetNumber: "AST-QR-TEST",
            Name: "Optical Level & Tripod Kit",
            Description: "Surveying level kit",
            SerialNumber: "SRV-112233",
            InternalCode: null,
            Barcode: null,
            RfidTag: null,
            CategoryId: cat.Id,
            AssetTypeId: type.Id,
            Make: "Leica",
            Model: "NA720",
            Year: "2024"));

        // Act 1: Generate QR Token
        var tokenRecord = await identifierService.GenerateQrTokenAsync(asset.Id);
        Assert.NotNull(tokenRecord.PublicToken);

        // Act 2: Generate SVG and PNG barcodes
        var svg = qrService.GenerateSvgQrCode($"https://app.connectedops.io/assets/scan/{tokenRecord.PublicToken}");
        var png = qrService.GeneratePngQrCode($"https://app.connectedops.io/assets/scan/{tokenRecord.PublicToken}");

        Assert.NotEmpty(svg);
        Assert.NotEmpty(png);
        Assert.Contains("<svg", svg);

        // Act 3: Public Scan Lookup
        var scan = await identifierService.ScanByTokenAsync(tokenRecord.PublicToken);
        Assert.NotNull(scan);
        Assert.Equal("Optical Level & Tripod Kit", scan.Name);
        Assert.Equal("AST-QR-TEST", scan.AssetNumber);
        Assert.Equal(AssetStatus.Available, scan.Status);
    }

    [Fact]
    public async Task AssetDashboardAndUtilizationService_CalculatesMetricsCorrectly()
    {
        // Arrange
        var (db, context, tenantId, _) = CreateTestEnv();
        var auditLog = new TestAuditLogService();
        var (cat, type) = await SeedCategoryAndTypeAsync(db, tenantId);
        var assetService = new AssetService(db, context, auditLog);
        var dashboardService = new AssetDashboardService(db, context);
        var utilizationService = new AssetUtilizationService(db, context);

        await assetService.CreateAsync(new CreateAssetRequest("AST-DASH-1", "Asset One", null, null, null, null, null, cat.Id, type.Id, "MakeA", "ModA", null, PurchaseCost: 1000m));
        await assetService.CreateAsync(new CreateAssetRequest("AST-DASH-2", "Asset Two", null, null, null, null, null, cat.Id, type.Id, "MakeB", "ModB", null, PurchaseCost: 2500m));

        // Act
        var dashboard = await dashboardService.GetDashboardSummaryAsync();
        var utilization = await utilizationService.GetUtilizationSummaryAsync();

        // Assert
        Assert.Equal(2, dashboard.TotalAssets);
        Assert.Equal(2, dashboard.AvailableAssets);
        Assert.Equal(3500m, dashboard.TotalAssetValue);
        Assert.Equal(2, utilization.TotalAssets);
        Assert.Equal(2, utilization.AvailableCount);
    }
}
