using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Drivers;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class DriverEligibilityServiceTests
{
    private static async Task<(Driver driver, Vehicle vehicle)> SetupDriverAndVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        DriverStatus driverStatus = DriverStatus.Active,
        bool hasValidLicense = true,
        bool isLicenseExpired = false,
        VehicleStatus vehicleStatus = VehicleStatus.Active)
    {
        var category = new VehicleCategory(tenantId, "Trucks", "TRK", "Cargo", true);
        var make = new VehicleMake(tenantId, "MAN", "DE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "TGX", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId, "VH-ELIG", category.Id, make.Id, model.Id, "MAN TGX 500",
            registrationNumber: "ABC-999", status: vehicleStatus);
        db.Vehicles.Add(vehicle);

        var driver = new Driver(
            tenantId, "DRV-ELIG", "Marcus", "Vance", "+1-555-0999", DriverType.Employee,
            null, null, null, null, driverStatus);
        db.Drivers.Add(driver);
        await db.SaveChangesAsync();

        if (hasValidLicense)
        {
            var expiry = isLicenseExpired
                ? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
                : DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1));

            var license = new DriverLicense(
                tenantId, driver.Id, "LIC-ELIG-1", "US", "CA DMV", null, expiry, isPrimary: true);
            db.DriverLicenses.Add(license);
            await db.SaveChangesAsync();
        }

        return (driver, vehicle);
    }

    [Fact]
    public async Task EvaluateAsync_ActiveDriverWithValidLicenseAndActiveVehicle_ReturnsEligible()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle) = await SetupDriverAndVehicleAsync(db, tenantId);

        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle.Id);

        Assert.NotNull(result);
        Assert.True(result.IsEligible);
        Assert.Empty(result.Reasons);
        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal(vehicle.Id, result.VehicleId);
    }

    [Fact]
    public async Task EvaluateAsync_InactiveDriver_ReturnsIneligibleWithReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle) = await SetupDriverAndVehicleAsync(db, tenantId, driverStatus: DriverStatus.Inactive);

        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle.Id);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Reasons, r => r.Contains("Inactive", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_SuspendedDriver_ReturnsIneligibleWithReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle) = await SetupDriverAndVehicleAsync(db, tenantId, driverStatus: DriverStatus.Suspended);

        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle.Id);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Reasons, r => r.Contains("suspended", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_ExpiredLicense_ReturnsIneligibleWithReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle) = await SetupDriverAndVehicleAsync(db, tenantId, isLicenseExpired: true);

        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle.Id);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Reasons, r => r.Contains("expired", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_OutOfServiceVehicle_ReturnsIneligibleWithReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle) = await SetupDriverAndVehicleAsync(db, tenantId, vehicleStatus: VehicleStatus.OutOfService);

        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle.Id);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Reasons, r => r.Contains("OutOfService", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task EvaluateAsync_DriverAlreadyHasActivePrimaryAssignment_ReturnsIneligibleWithReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var eligibilityService = new DriverEligibilityService(db, userContext);

        var (driver, vehicle1) = await SetupDriverAndVehicleAsync(db, tenantId);

        // Assign driver to vehicle 1
        var assignment = new DriverVehicleAssignment(
            tenantId, driver.Id, vehicle1.Id, AssignmentType.Primary,
            DateTime.UtcNow.AddDays(-1), null, isPrimary: true);
        db.DriverVehicleAssignments.Add(assignment);
        await db.SaveChangesAsync();

        // Create second vehicle
        var (category, make, model, vehicle2Id) = await CreateSecondVehicleAsync(db, tenantId);

        // Check eligibility for vehicle 2
        var result = await eligibilityService.EvaluateAsync(driver.Id, vehicle2Id);

        Assert.False(result.IsEligible);
        Assert.Contains(result.Reasons, r => r.Contains("already actively assigned", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(VehicleCategory, VehicleMake, VehicleModel, Guid)> CreateSecondVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId)
    {
        var category = new VehicleCategory(tenantId, "Vans", "VAN", "Delivery", true);
        var make = new VehicleMake(tenantId, "Mercedes", "DE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Sprinter", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle2 = new Vehicle(
            tenantId, "VH-002", category.Id, make.Id, model.Id, "Sprinter 314",
            registrationNumber: "XYZ-789", status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle2);
        await db.SaveChangesAsync();

        return (category, make, model, vehicle2.Id);
    }
}
