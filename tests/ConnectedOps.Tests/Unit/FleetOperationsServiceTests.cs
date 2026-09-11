using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.FleetOperations;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class FleetOperationsServiceTests
{
    private static async Task<(Vehicle Vehicle, Driver Driver)> SeedVehicleAndDriverAsync(
        ConnectedOpsDbContext db,
        Guid tenantId)
    {
        var category = new VehicleCategory(tenantId, "Van", "VAN", null, true);
        var make = new VehicleMake(tenantId, "Ford", "US");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Transit", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            "FLT-100",
            category.Id,
            make.Id,
            model.Id,
            displayName: "Transit 100",
            registrationNumber: "ABC-1234",
            currentOdometer: 10000m,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);

        var driver = new Driver(
            tenantId,
            "DRV-001",
            "John",
            "Doe",
            "+1-555-0101",
            DriverType.Employee,
            null,
            null,
            null,
            null,
            DriverStatus.Active);
        db.Drivers.Add(driver);

        await db.SaveChangesAsync();
        return (vehicle, driver);
    }

    [Fact]
    public async Task ShiftService_CreateShift_PersistsAndLogsAudit()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var shiftService = new FleetShiftService(db, userContext, auditService);

        var request = new CreateFleetShiftRequest(
            "Morning Shift",
            "SFT-MORN",
            new TimeOnly(6, 0),
            new TimeOnly(14, 0),
            DayOfWeekFlags.Weekdays,
            null,
            "Standard morning operations");

        var result = await shiftService.CreateShiftAsync(request);

        Assert.NotNull(result);
        Assert.Equal("Morning Shift", result.Name);
        Assert.Equal("SFT-MORN", result.Code);
        Assert.True(result.IsActive);
        Assert.Single(auditService.WrittenLogs);
        Assert.Equal(AuditAction.FleetShiftCreated, auditService.WrittenLogs[0].Action);
    }

    [Fact]
    public async Task ShiftService_DeactivateAndActivate_UpdatesState()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var shiftService = new FleetShiftService(db, userContext, auditService);

        var shift = await shiftService.CreateShiftAsync(new CreateFleetShiftRequest(
            "Night Shift",
            "SFT-NIGHT",
            new TimeOnly(22, 0),
            new TimeOnly(6, 0)));

        await shiftService.DeactivateShiftAsync(shift.Id);
        var deactivated = await shiftService.GetShiftByIdAsync(shift.Id);
        Assert.False(deactivated.IsActive);

        await shiftService.ActivateShiftAsync(shift.Id);
        var activated = await shiftService.GetShiftByIdAsync(shift.Id);
        Assert.True(activated.IsActive);
    }

    [Fact]
    public async Task UsageSession_Checkout_Success_OpensSessionAndRecordsCondition()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var request = new CreateCheckoutRequest(
            vehicle.Id,
            driver.Id,
            10500m,
            OdometerUnit.Kilometers,
            DateTime.UtcNow,
            null,
            null,
            null,
            VehicleCondition.Good,
            "Daily Delivery",
            "TRP-001");

        var result = await sessionService.CheckoutVehicleAsync(request);

        Assert.NotNull(result);
        Assert.Equal(UsageSessionStatus.Open, result.Status);
        Assert.Equal(10500m, result.StartOdometer);
        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal(vehicle.Id, result.VehicleId);

        // Verify condition record
        Assert.Single(db.VehicleConditionRecords);
        var condition = db.VehicleConditionRecords.First();
        Assert.Equal(VehicleCondition.Good, condition.Condition);
        Assert.Equal(10500m, condition.Odometer);

        // Verify audit log
        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.VehicleCheckedOut);
    }

    [Fact]
    public async Task UsageSession_Checkout_WhenVehicleAlreadyOpen_ThrowsInvalidOperationException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var request = new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m);
        await sessionService.CheckoutVehicleAsync(request);

        // Second driver attempts checkout on the same vehicle while first is still Open
        var secondDriver = new Driver(tenantId, "DRV-002", "Jane", "Smith", "+1-555-0102", DriverType.Employee, null, null, null, null, DriverStatus.Active);
        db.Drivers.Add(secondDriver);
        await db.SaveChangesAsync();

        var duplicateRequest = new CreateCheckoutRequest(vehicle.Id, secondDriver.Id, 10000m);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sessionService.CheckoutVehicleAsync(duplicateRequest));
        Assert.Contains("already checked out", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UsageSession_Checkout_WhenDriverIneligible_ThrowsAndRecordsOperationalException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService
        {
            IsEligible = false,
            Reasons = ["Driver license has expired", "Driver lacks heavy vehicle certification"]
        };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var request = new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sessionService.CheckoutVehicleAsync(request));
        Assert.Contains("eligibility check failed", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify operational exception was logged
        Assert.Single(db.FleetOperationalExceptions);
        var opEx = db.FleetOperationalExceptions.First();
        Assert.Equal(OperationalExceptionType.DriverUnavailable, opEx.ExceptionType);
        Assert.Equal(OperationalExceptionSeverity.High, opEx.Severity);
    }

    [Fact]
    public async Task UsageSession_CheckIn_Success_CompletesSessionAndRecordsOdometer()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var checkout = await sessionService.CheckoutVehicleAsync(new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m));

        var checkInRequest = new CheckInSessionRequest(
            EndOdometer: 10250m,
            Condition: VehicleCondition.Good,
            Notes: "Return inspection OK");

        var completed = await sessionService.CheckInVehicleAsync(checkout.Id, checkInRequest);

        Assert.Equal(UsageSessionStatus.Completed, completed.Status);
        Assert.Equal(10250m, completed.EndOdometer);
        Assert.Equal(250m, completed.DistanceTraveled);

        // Verify Phase 3 odometer service was invoked
        Assert.Single(odoService.RecordedReadings);
        Assert.Equal(10250m, odoService.RecordedReadings[0].Request.Reading);

        // Verify audit log
        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.VehicleCheckedIn);
    }

    [Fact]
    public async Task UsageSession_CheckIn_EndOdometerLessThanStart_ThrowsAndRecordsOdometerMismatch()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var checkout = await sessionService.CheckoutVehicleAsync(new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m));

        var checkInRequest = new CheckInSessionRequest(
            EndOdometer: 9500m, // Invalid: less than start 10000m
            Condition: VehicleCondition.Good);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sessionService.CheckInVehicleAsync(checkout.Id, checkInRequest));
        Assert.Contains("cannot be less", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify operational exception created
        Assert.Single(db.FleetOperationalExceptions);
        var opEx = db.FleetOperationalExceptions.First();
        Assert.Equal(OperationalExceptionType.OdometerMismatch, opEx.ExceptionType);
    }

    [Fact]
    public async Task UsageSession_CheckIn_AttentionRequiredCondition_RecordsOperationalException()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        var checkout = await sessionService.CheckoutVehicleAsync(new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m));

        var checkInRequest = new CheckInSessionRequest(
            EndOdometer: 10100m,
            Condition: VehicleCondition.AttentionRequired,
            Notes: "Cracked passenger side mirror");

        var completed = await sessionService.CheckInVehicleAsync(checkout.Id, checkInRequest);
        Assert.Equal(UsageSessionStatus.Completed, completed.Status);

        // Verify operational exception logged
        Assert.Single(db.FleetOperationalExceptions);
        var opEx = db.FleetOperationalExceptions.First();
        Assert.Equal(OperationalExceptionType.VehicleConditionIssue, opEx.ExceptionType);
        Assert.Equal(OperationalExceptionSeverity.High, opEx.Severity);
    }

    [Fact]
    public async Task VehicleHandover_TransfersCustodyAndStartsNewSession()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver1) = await SeedVehicleAndDriverAsync(db, tenantId);
        var driver2 = new Driver(tenantId, "DRV-002", "Jane", "Smith", "+1-555-0102", DriverType.Employee, null, null, null, null, DriverStatus.Active);
        db.Drivers.Add(driver2);
        await db.SaveChangesAsync();

        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);
        var handoverService = new VehicleHandoverService(db, userContext, auditService, odoService, eligService);

        // Driver 1 checks out vehicle
        var initialSession = await sessionService.CheckoutVehicleAsync(new CreateCheckoutRequest(vehicle.Id, driver1.Id, 10000m));

        // Driver 1 hands over vehicle in the field to Driver 2 at 10150km
        var handover = await handoverService.CreateHandoverAsync(new CreateVehicleHandoverRequest(
            VehicleId: vehicle.Id,
            ToDriverId: driver2.Id,
            Odometer: 10150m,
            Notes: "Mid-day handover"));

        Assert.NotNull(handover);
        Assert.Equal(driver1.Id, handover.FromDriverId);
        Assert.Equal(driver2.Id, handover.ToDriverId);
        Assert.Equal(10150m, handover.Odometer);

        // Verify initial session was closed
        var closedSession = await sessionService.GetSessionByIdAsync(initialSession.Id);
        Assert.Equal(UsageSessionStatus.Completed, closedSession.Status);
        Assert.Equal(10150m, closedSession.EndOdometer);

        // Verify new active session was opened for Driver 2
        var activeSession = await sessionService.GetActiveSessionForVehicleAsync(vehicle.Id);
        Assert.NotNull(activeSession);
        Assert.Equal(driver2.Id, activeSession.DriverId);
        Assert.Equal(10150m, activeSession.StartOdometer);

        // Verify audit log
        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.VehicleHandedOver);
    }

    [Fact]
    public async Task FleetAvailability_CalculatesCorrectStatuses()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var auditService = new TestAuditLogService();
        var odoService = new TestOdometerService();
        var eligService = new TestDriverEligibilityService { IsEligible = true };

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var availabilityService = new FleetAvailabilityService(db, userContext);
        var sessionService = new VehicleUsageSessionService(db, userContext, auditService, odoService, eligService);

        // Initially: vehicle is Available, driver is Available
        var initialVeh = await availabilityService.GetVehicleAvailabilityAsync(vehicle.Id);
        Assert.Equal(VehicleAvailabilityStatus.Available, initialVeh.AvailabilityStatus);

        var initialDrv = await availabilityService.GetDriverAvailabilityAsync(driver.Id);
        Assert.Equal(DriverAvailabilityStatus.Available, initialDrv.AvailabilityStatus);

        // After Checkout: vehicle is CheckedOut, driver is OnTrip (Assigned)
        await sessionService.CheckoutVehicleAsync(new CreateCheckoutRequest(vehicle.Id, driver.Id, 10000m));

        var inUseVeh = await availabilityService.GetVehicleAvailabilityAsync(vehicle.Id);
        Assert.Equal(VehicleAvailabilityStatus.CheckedOut, inUseVeh.AvailabilityStatus);
        Assert.False(inUseVeh.IsAvailable);
        Assert.Equal(driver.Id, inUseVeh.CurrentDriverId);

        var inUseDrv = await availabilityService.GetDriverAvailabilityAsync(driver.Id);
        Assert.Equal(DriverAvailabilityStatus.Assigned, inUseDrv.AvailabilityStatus);
        Assert.False(inUseDrv.IsAvailable);
    }

    [Fact]
    public async Task OperationalExceptions_ResolveAndDismiss_UpdatesStatus()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = userId };
        var auditService = new TestAuditLogService();
        var exceptionService = new FleetOperationalExceptionService(db, userContext, auditService);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var created = await exceptionService.CreateExceptionAsync(new CreateOperationalExceptionRequest(
            OperationalExceptionType.VehicleConditionIssue,
            OperationalExceptionSeverity.High,
            "Broken tail light reported",
            vehicle.Id,
            driver.Id));

        Assert.Equal(OperationalExceptionStatus.Open, created.Status);

        // Resolve
        var resolved = await exceptionService.ResolveExceptionAsync(created.Id, new ResolveOperationalExceptionRequest("Tail light replaced by workshop"));
        Assert.Equal(OperationalExceptionStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAtUtc);
        Assert.Equal("Tail light replaced by workshop", resolved.ResolutionNotes);

        Assert.Contains(auditService.WrittenLogs, l => l.Action == AuditAction.FleetOperationalExceptionResolved);
    }
}

public sealed class TestOdometerService : IVehicleOdometerService
{
    public List<(Guid VehicleId, RecordVehicleOdometerRequest Request)> RecordedReadings { get; } = [];

    public Task<IReadOnlyCollection<VehicleOdometerEntryDto>> GetOdometerHistoryAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<VehicleOdometerEntryDto>>([]);

    public Task<VehicleOdometerEntryDto> RecordOdometerAsync(Guid vehicleId, RecordVehicleOdometerRequest request, CancellationToken cancellationToken = default)
    {
        RecordedReadings.Add((vehicleId, request));
        return Task.FromResult(new VehicleOdometerEntryDto(
            Guid.NewGuid(),
            vehicleId,
            request.Reading,
            request.Unit,
            request.Unit.ToString(),
            request.ReadingDateUtc,
            request.Source,
            request.Source.ToString(),
            request.Notes,
            Guid.NewGuid(),
            DateTime.UtcNow));
    }
}

public sealed class TestDriverEligibilityService : IDriverEligibilityService
{
    public bool IsEligible { get; set; } = true;
    public List<string> Reasons { get; set; } = [];

    public Task<DriverEligibilityResult> EvaluateAsync(Guid driverId, Guid vehicleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new DriverEligibilityResult(
            IsEligible,
            Reasons,
            driverId,
            "Test Driver",
            vehicleId,
            "FLT-001"));
}
