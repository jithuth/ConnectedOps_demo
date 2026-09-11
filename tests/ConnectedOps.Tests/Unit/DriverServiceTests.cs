using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Drivers;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class DriverServiceTests
{
    private static async Task<(Guid CategoryId, Guid MakeId, Guid ModelId, Guid VehicleId)> SeedVehiclePrerequisitesAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId)
    {
        var category = new VehicleCategory(tenantId, "Trucks", "TRK", "Cargo transport", true);
        var make = new VehicleMake(tenantId, "Volvo", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "FH16", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId, "VH-001", category.Id, make.Id, model.Id, "Volvo Heavy",
            registrationNumber: "ABC-123");
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return (category.Id, make.Id, model.Id, vehicle.Id);
    }

    [Fact]
    public async Task CreateDriverAsync_ValidRequest_CreatesDriverAndInitialLicense()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = userId, Email = "ops@connectedops.io" };
        var auditService = new TestAuditLogService();
        var driverService = new DriverService(db, userContext, auditService);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new CreateDriverRequest(
            DriverNumber: "DRV-101",
            FirstName: "John",
            LastName: "Doe",
            Phone: "+1-555-0101",
            DriverType: DriverType.Employee,
            Email: "johndoe@fleet.com",
            InitialLicenseNumber: "DL-888999",
            InitialLicenseCountryCode: "US",
            InitialLicenseExpiryDate: today.AddYears(2),
            InitialLicenseAuthority: "CA DMV",
            InitialLicenseCategoryCode: "Class A");

        var driver = await driverService.CreateDriverAsync(request);

        Assert.NotNull(driver);
        Assert.Equal("DRV-101", driver.DriverNumber);
        Assert.Equal("John Doe", driver.DisplayName);
        Assert.Equal(DriverStatus.Active, driver.Status);
        Assert.Equal(DriverType.Employee, driver.DriverType);

        // Verify license was created and marked primary
        Assert.Single(driver.Licenses);
        var license = driver.Licenses.First();
        Assert.Equal("DL-888999", license.LicenseNumber);
        Assert.True(license.IsPrimary);
        Assert.True(license.IsActive);
        Assert.False(license.IsExpired);
        Assert.Single(license.Categories);
        Assert.Equal("CLASS A", license.Categories.First().CategoryCode);

        // Verify audit log
        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.Created && l.EntityType == "Driver");
    }

    [Fact]
    public async Task CreateDriverAsync_DuplicateDriverNumber_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var driverService = new DriverService(db, userContext, auditService);

        var req1 = new CreateDriverRequest("DRV-DUP", "Jane", "Smith", "+1-555-0102", DriverType.Employee);
        await driverService.CreateDriverAsync(req1);

        var req2 = new CreateDriverRequest("DRV-DUP", "Another", "Driver", "+1-555-0103", DriverType.Contractor);
        await Assert.ThrowsAsync<ConflictException>(() => driverService.CreateDriverAsync(req2));
    }

    [Fact]
    public async Task CreateDriverAsync_DuplicateEmployeeLink_ThrowsConflictException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var driverService = new DriverService(db, userContext, auditService);

        // Create an employee
        var employee = new Employee(tenantId, "EMP-001", "Alice", "Wonder", "alice@company.com");
        db.Employees.Add(employee);
        await db.SaveChangesAsync();

        var req1 = new CreateDriverRequest("DRV-EMP1", "Alice", "Wonder", "+1-555-0104", DriverType.Employee, EmployeeId: employee.Id);
        await driverService.CreateDriverAsync(req1);

        var req2 = new CreateDriverRequest("DRV-EMP2", "Alice", "Clone", "+1-555-0105", DriverType.Employee, EmployeeId: employee.Id);
        await Assert.ThrowsAsync<ConflictException>(() => driverService.CreateDriverAsync(req2));
    }

    [Fact]
    public async Task TenantIsolation_CannotAccessDriverFromOtherTenant()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();

        var driverServiceA = new DriverService(db, userContextA, auditService);
        var driverServiceB = new DriverService(db, userContextB, auditService);

        var driverA = await driverServiceA.CreateDriverAsync(
            new CreateDriverRequest("DRV-A", "Tenant", "A", "+1-555-0001", DriverType.Employee));

        // Tenant B must not be able to find Tenant A's driver
        await Assert.ThrowsAsync<KeyNotFoundException>(() => driverServiceB.GetDriverByIdAsync(driverA.Id));

        // Tenant B's paged result must be empty
        var pagedB = await driverServiceB.GetDriversPagedAsync(new DriverQueryParameters());
        Assert.Empty(pagedB.Items);
    }

    [Fact]
    public async Task DriverLicense_SetPrimaryLicense_MaintainsSinglePrimaryInvariant()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var driverService = new DriverService(db, userContext, auditService);
        var licenseService = new DriverLicenseService(db, userContext, auditService);

        var driver = await driverService.CreateDriverAsync(
            new CreateDriverRequest("DRV-LIC-TEST", "Robert", "Frost", "+1-555-0199", DriverType.Employee));

        var lic1 = await licenseService.AddLicenseAsync(driver.Id, new CreateDriverLicenseRequest("LIC-001", "US", IsPrimary: true));
        var lic2 = await licenseService.AddLicenseAsync(driver.Id, new CreateDriverLicenseRequest("LIC-002", "US", IsPrimary: false));

        // Initially lic1 is primary
        var d1 = await driverService.GetDriverByIdAsync(driver.Id);
        Assert.Equal("LIC-001", d1.Licenses.First(l => l.IsPrimary).LicenseNumber);

        // Switch primary to lic2
        await licenseService.SetPrimaryLicenseAsync(driver.Id, lic2.Id);

        var d2 = await driverService.GetDriverByIdAsync(driver.Id);
        var primaryLicenses = d2.Licenses.Where(l => l.IsPrimary).ToList();
        Assert.Single(primaryLicenses);
        Assert.Equal("LIC-002", primaryLicenses.First().LicenseNumber);
    }

    [Fact]
    public void DriverDocument_ExpiryCalculation_WorksCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expiredDoc = new DriverDocument(
            tenantId, driverId, DriverDocumentType.MedicalFitness, "DOT Medical Card 2023",
            "MED-001", today.AddYears(-2), today.AddDays(-10), "Clinic",
            "keys/med.pdf", "med.pdf", "application/pdf", 1024, null);

        var expiringSoonDoc = new DriverDocument(
            tenantId, driverId, DriverDocumentType.MedicalFitness, "DOT Medical Card 2026",
            "MED-002", today.AddYears(-1), today.AddDays(15), "Clinic",
            "keys/med2.pdf", "med2.pdf", "application/pdf", 1024, null);

        var validDoc = new DriverDocument(
            tenantId, driverId, DriverDocumentType.TrainingCertificate, "Safety Certificate",
            "SAF-001", today, today.AddDays(200), "Safety Academy",
            "keys/saf.pdf", "saf.pdf", "application/pdf", 1024, null);

        Assert.True(expiredDoc.IsExpired());
        Assert.False(expiredDoc.IsExpiringSoon()); // already expired

        Assert.False(expiringSoonDoc.IsExpired());
        Assert.True(expiringSoonDoc.IsExpiringSoon());

        Assert.False(validDoc.IsExpired());
        Assert.False(validDoc.IsExpiringSoon());
    }

    [Fact]
    public void DriverCertification_ExpiryCalculation_WorksCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var expiredCert = new DriverCertification(
            tenantId, driverId, CertificationType.HazardousMaterials, "Hazmat Endorsement",
            "HAZ-123", "DOT", today.AddYears(-3), today.AddDays(-20), null, null);

        var expiringSoonCert = new DriverCertification(
            tenantId, driverId, CertificationType.DefensiveDriving, "Defensive Driving",
            "DEF-456", "NSC", today.AddYears(-1), today.AddDays(10), null, null);

        var validCert = new DriverCertification(
            tenantId, driverId, CertificationType.FirstAid, "First Aid & CPR",
            "FA-789", "Red Cross", today, today.AddYears(1), null, null);

        Assert.True(expiredCert.IsExpired());
        Assert.False(expiredCert.IsExpiringSoon());

        Assert.False(expiringSoonCert.IsExpired());
        Assert.True(expiringSoonCert.IsExpiringSoon());

        Assert.False(validCert.IsExpired());
        Assert.False(validCert.IsExpiringSoon());
    }

    [Fact]
    public async Task DriverAssignment_CreateAndEndAssignment_SavesHistoryProperly()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var driverService = new DriverService(db, userContext, auditService);
        var licenseService = new DriverLicenseService(db, userContext, auditService);
        var eligibilityService = new DriverEligibilityService(db, userContext);
        var assignmentService = new DriverAssignmentService(db, userContext, auditService, eligibilityService);

        var (_, _, _, vehicleId) = await SeedVehiclePrerequisitesAsync(db, tenantId);
        var driver = await driverService.CreateDriverAsync(
            new CreateDriverRequest("DRV-ASSIGN", "Samuel", "Jackson", "+1-555-0800", DriverType.Employee));

        // Add valid license to driver so eligibility passes
        await licenseService.AddLicenseAsync(driver.Id, new CreateDriverLicenseRequest(
            "LIC-ASSIGN-1", "US", ExpiryDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)), IsPrimary: true));

        // Create assignment
        var assignment = await assignmentService.CreateAssignmentAsync(
            new CreateDriverVehicleAssignmentRequest(
                driver.Id,
                vehicleId,
                AssignmentType.Primary,
                DateTime.UtcNow.AddHours(-2),
                null,
                true,
                "Regular daily route"));

        Assert.NotNull(assignment);
        Assert.True(assignment.IsActive);
        Assert.True(assignment.IsPrimary);

        // Driver details must now reflect this assignment
        var activeDriver = await driverService.GetDriverByIdAsync(driver.Id);
        Assert.NotNull(activeDriver.CurrentVehicleAssignment);
        Assert.Equal(vehicleId, activeDriver.CurrentVehicleAssignment.VehicleId);

        // End assignment
        var ended = await assignmentService.EndAssignmentAsync(
            assignment.Id,
            new EndDriverVehicleAssignmentRequest("Route completed", DateTime.UtcNow));

        Assert.False(ended.IsActive);
        Assert.NotNull(ended.AssignedToUtc);

        // Driver details must now have null current assignment but 1 in recent history
        var freeDriver = await driverService.GetDriverByIdAsync(driver.Id);
        Assert.Null(freeDriver.CurrentVehicleAssignment);
        Assert.Single(freeDriver.RecentAssignments);
    }
}
