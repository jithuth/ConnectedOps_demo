using ConnectedOps.Application.Expenses;
using ConnectedOps.Application.Gamification;
using ConnectedOps.Application.Hos;
using ConnectedOps.Application.Tracking;
using ConnectedOps.Domain.Dispatch;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Expenses;
using ConnectedOps.Domain.Gamification;
using ConnectedOps.Domain.Hos;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Application.Demo;
using ConnectedOps.Infrastructure.Demo;
using ConnectedOps.Infrastructure.Expenses;
using ConnectedOps.Infrastructure.Gamification;
using ConnectedOps.Infrastructure.Hos;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Tracking;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class Phase15DriverExperienceTests
{
    private static async Task<(Vehicle Vehicle, Driver Driver)> SeedVehicleAndDriverAsync(
        ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "VAN-01",
        string driverNumber = "DRV-100")
    {
        var category = new VehicleCategory(tenantId, "Van", "VAN", null, true);
        var make = new VehicleMake(tenantId, "Toyota", "JP");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "HiAce", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            vehicleNumber,
            category.Id,
            make.Id,
            model.Id,
            displayName: "HiAce 1",
            registrationNumber: "DXB-9988",
            currentOdometer: 15000m,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);

        var driver = new Driver(
            tenantId,
            driverNumber,
            "Tariq",
            "Mansoor",
            "+971509998877",
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
    public async Task HosPolicy_Customization_AndPresets_ShouldApplyCorrectLimits()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var hosService = new HosService(db, userContext);

        // 1. Initial default policy should be GCC / UAE Standard
        var defaultPolicy = await hosService.GetPolicyAsync();
        Assert.Equal(HosPresetType.GccUaeStandard, defaultPolicy.PresetType);
        Assert.Equal(10.0, defaultPolicy.MaxDrivingHoursPerShift);
        Assert.Equal(12.0, defaultPolicy.MaxShiftDutyHours);
        Assert.Equal(4.5, defaultPolicy.DriveHoursBeforeMandatoryBreak);
        Assert.Equal(45, defaultPolicy.MandatoryBreakMinutes);

        // 2. Switch to EU Tachograph preset
        var euPolicy = await hosService.UpdatePolicyAsync(new UpdateHosPolicyRequest(
            PresetType: HosPresetType.EuTachograph,
            MaxDrivingHoursPerShift: 9.0,
            MaxShiftDutyHours: 13.0,
            DriveHoursBeforeMandatoryBreak: 4.5,
            MandatoryBreakMinutes: 45,
            MinConsecutiveOffDutyHours: 11.0,
            CycleDays: 6,
            CycleMaxDutyHours: 56.0));

        Assert.Equal(HosPresetType.EuTachograph, euPolicy.PresetType);
        Assert.Equal(9.0, euPolicy.MaxDrivingHoursPerShift);
        Assert.Equal(45, euPolicy.MandatoryBreakMinutes);

        // 3. Switch to Custom Company Policy
        var customPolicy = await hosService.UpdatePolicyAsync(new UpdateHosPolicyRequest(
            PresetType: HosPresetType.Custom,
            MaxDrivingHoursPerShift: 8.0,
            MaxShiftDutyHours: 12.0,
            DriveHoursBeforeMandatoryBreak: 3.5,
            MandatoryBreakMinutes: 40,
            MinConsecutiveOffDutyHours: 12.0,
            CycleDays: 5,
            CycleMaxDutyHours: 40.0));

        Assert.Equal(HosPresetType.Custom, customPolicy.PresetType);
        Assert.Equal(8.0, customPolicy.MaxDrivingHoursPerShift);
        Assert.Equal(3.5, customPolicy.DriveHoursBeforeMandatoryBreak);
    }

    [Fact]
    public async Task HosDutyStatus_Transitions_AndClocks_ShouldComputeCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var hosService = new HosService(db, userContext);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // Transition 1: On Duty (Not Driving) at depot
        var entry1 = await hosService.ChangeDutyStatusAsync(driver.Id, new ChangeDutyStatusRequest(
            Status: DutyStatus.OnDutyNotDriving,
            VehicleId: vehicle.Id,
            Odometer: 15000m,
            LocationName: "Dubai Depot",
            Notes: "Pre-trip vehicle inspection"));

        Assert.Equal(DutyStatus.OnDutyNotDriving, entry1.Status);

        // Transition 2: Driving
        var entry2 = await hosService.ChangeDutyStatusAsync(driver.Id, new ChangeDutyStatusRequest(
            Status: DutyStatus.Driving,
            VehicleId: vehicle.Id,
            Odometer: 15005m,
            LocationName: "Sheikh Zayed Rd"));

        Assert.Equal(DutyStatus.Driving, entry2.Status);

        // Calculate clocks
        var clocks = await hosService.GetDriverClocksAsync(driver.Id);
        Assert.Equal(DutyStatus.Driving, clocks.CurrentStatus);
        Assert.True(clocks.RemainingDrivingHours > 0);
        Assert.True(clocks.RemainingShiftDutyHours > 0);
    }

    [Fact]
    public async Task HosViolation_Detection_ShouldFlagExcessiveDriving()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var hosService = new HosService(db, userContext);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // Seed a driving log that started 12 hours ago (exceeding 10h GCC driving limit)
        var oldDrivingLog = new HosLogEntry(
            tenantId,
            driver.Id,
            DutyStatus.Driving,
            DateTime.UtcNow.AddHours(-12),
            vehicleId: vehicle.Id,
            startOdometer: 15000m);
        db.HosLogEntries.Add(oldDrivingLog);
        await db.SaveChangesAsync();

        // Calculate clocks to trigger evaluation
        var clocks = await hosService.GetDriverClocksAsync(driver.Id);

        // Should have violations
        Assert.NotEmpty(clocks.ActiveViolations);
        var violation = clocks.ActiveViolations.First();
        Assert.Equal(HosViolationType.DrivingLimitExceeded, violation.ViolationType);

        // Acknowledge violation
        var ack = await hosService.AcknowledgeViolationAsync(violation.Id);
        Assert.True(ack.IsAcknowledged);
    }

    [Fact]
    public async Task Gamification_Scorecard_Recalculation_AndTier_Ranking()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var gamificationService = new GamificationService(db, userContext, NullLogger<GamificationService>.Instance);

        var (v1, d1) = await SeedVehicleAndDriverAsync(db, tenantId, "VAN-01", "DRV-101");
        var (v2, d2) = await SeedVehicleAndDriverAsync(db, tenantId, "VAN-02", "DRV-102");

        var now = DateTime.UtcNow;

        // Recalculate scorecards
        var processed = await gamificationService.RecalculateAllScorecardsAsync(now.Month, now.Year);
        Assert.Equal(2, processed);

        // Get Leaderboard
        var leaderboard = await gamificationService.GetLeaderboardAsync(now.Month, now.Year);
        Assert.Equal(2, leaderboard.TotalRankedDrivers);
        Assert.NotEmpty(leaderboard.Rankings);

        // Award badge to top performer
        var badge = await gamificationService.AwardBadgeAsync(new AwardBadgeRequest(
            DriverId: d1.Id,
            BadgeType: BadgeType.SafetyMaster,
            Title: "Master of Road Safety",
            Description: "Flawless compliance record",
            Points: 200));

        Assert.Equal("Master of Road Safety", badge.Title);
        Assert.Equal(200, badge.Points);

        var badges = await gamificationService.GetDriverBadgesAsync(d1.Id);
        Assert.Contains(badges, b => b.Title == "Master of Road Safety");
    }

    [Fact]
    public async Task DriverExpense_Lifecycle_Submit_Approve_Reimburse()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var expenseService = new DriverExpenseService(db, userContext, NullLogger<DriverExpenseService>.Instance);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // 1. Submit roadside tire repair expense
        var expense = await expenseService.SubmitExpenseAsync(new SubmitDriverExpenseRequest(
            DriverId: driver.Id,
            Category: ExpenseCategory.TireRepair,
            Amount: 180.50m,
            Description: "Emergency highway tire repair",
            IncurredAtUtc: DateTime.UtcNow,
            VehicleId: vehicle.Id,
            Currency: "AED"));

        Assert.Equal(ExpenseStatus.Submitted, expense.Status);
        Assert.Equal(180.50m, expense.Amount);

        // Check dashboard
        var dash = await expenseService.GetDashboardAsync();
        Assert.Equal(1, dash.PendingApprovalCount);
        Assert.Equal(180.50m, dash.TotalPendingApprovalAmount);

        // 2. Approve expense
        var approved = await expenseService.ApproveExpenseAsync(expense.Id, new ApproveExpenseRequest());
        Assert.Equal(ExpenseStatus.Approved, approved.Status);

        // 3. Mark Reimbursed
        var reimbursed = await expenseService.ReimburseExpenseAsync(expense.Id, new ReimburseExpenseRequest("PETTY-VOUCHER-998"));
        Assert.Equal(ExpenseStatus.Reimbursed, reimbursed.Status);
        Assert.Equal("PETTY-VOUCHER-998", reimbursed.ReimbursementReference);
    }

    [Fact]
    public async Task PublicTracking_Token_AndFeedback_CSAT()
    {
        var tenantId = Guid.NewGuid();
        var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var simulator = new TestDemoFleetSimulator();

        var trackingService = new PublicTrackingService(db, userContext, simulator, NullLogger<PublicTrackingService>.Instance);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // Create a dispatch job
        var job = new DispatchJob(
            tenantId,
            "JOB-7701",
            "Electronics Express Delivery",
            DispatchJobType.Delivery,
            DispatchJobPriority.High,
            "Emirates Logistics Customer",
            "+971505554433",
            "Dubai Marina Tower A",
            25.0772,
            55.1378);

        job.AssignToRoute(Guid.NewGuid(), vehicle.Id, driver.Id);
        db.DispatchJobs.Add(job);
        await db.SaveChangesAsync();

        // 1. Generate Token
        var tokenDto = await trackingService.GenerateTokenForJobAsync(new GenerateTrackingTokenRequest(job.Id, 48));
        Assert.NotNull(tokenDto.Token);
        Assert.False(tokenDto.IsExpired);

        // 2. Access public tracking anonymously
        var trackingInfo = await trackingService.GetPublicTrackingInfoAsync(tokenDto.Token);
        Assert.NotNull(trackingInfo);
        Assert.Equal("Emirates Logistics Customer", trackingInfo.CustomerName);
        Assert.Equal("Dubai Marina Tower A", trackingInfo.DeliveryAddress);
        Assert.NotNull(trackingInfo.VehiclePlateNumber);
        Assert.NotNull(trackingInfo.EstimatedMinutesRemaining);

        // 3. Submit 5-star feedback
        var ratingSubmitted = await trackingService.SubmitDeliveryRatingAsync(tokenDto.Token, new SubmitDeliveryRatingRequest(5, "Driver was prompt and polite!"));
        Assert.True(ratingSubmitted);

        // Verify rating is persisted
        var updatedInfo = await trackingService.GetPublicTrackingInfoAsync(tokenDto.Token);
        Assert.NotNull(updatedInfo);
        Assert.Equal(5, updatedInfo.Rating);
        Assert.Equal("Driver was prompt and polite!", updatedInfo.FeedbackComment);
    }

    [Fact]
    public async Task TenantIsolation_ShouldPreventCrossTenantAccess()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var db = TestDbContextFactory.Create();

        var (vA, dA) = await SeedVehicleAndDriverAsync(db, tenantA, "VAN-A", "DRV-A");
        var (vB, dB) = await SeedVehicleAndDriverAsync(db, tenantB, "VAN-B", "DRV-B");

        // Context for Tenant A
        var contextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var expenseServiceA = new DriverExpenseService(db, contextA, NullLogger<DriverExpenseService>.Instance);

        // Context for Tenant B
        var contextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var expenseServiceB = new DriverExpenseService(db, contextB, NullLogger<DriverExpenseService>.Instance);

        // Tenant A submits expense
        var expA = await expenseServiceA.SubmitExpenseAsync(new SubmitDriverExpenseRequest(
            DriverId: dA.Id,
            Category: ExpenseCategory.Toll,
            Amount: 20m,
            Description: "Salik Toll Gate",
            IncurredAtUtc: DateTime.UtcNow,
            VehicleId: vA.Id));

        // Tenant B queries expenses - should NOT see Tenant A's expense
        var pagedB = await expenseServiceB.GetExpensesPagedAsync(new ExpenseFilterRequest());
        Assert.DoesNotContain(pagedB.Items, x => x.Id == expA.Id);

        // Tenant B attempts to approve Tenant A's expense - should fail with KeyNotFoundException
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            expenseServiceB.ApproveExpenseAsync(expA.Id, new ApproveExpenseRequest()));
    }
}

public sealed class TestDemoFleetSimulator : IDemoFleetSimulator
{
    public Task<DemoFleetStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new DemoFleetStatusDto(false, false, 0, 0, 0, null, 0L, "OK"));

    public Task<DemoFleetStatusDto> CreateDemoFleetAsync(CreateDemoFleetRequest request, CancellationToken cancellationToken = default) =>
        GetStatusAsync(cancellationToken);

    public Task<DemoFleetStatusDto> StartSimulationAsync(CancellationToken cancellationToken = default) =>
        GetStatusAsync(cancellationToken);

    public Task<DemoFleetStatusDto> StopSimulationAsync(CancellationToken cancellationToken = default) =>
        GetStatusAsync(cancellationToken);

    public Task<DemoFleetStatusDto> ResetSimulationAsync(CancellationToken cancellationToken = default) =>
        GetStatusAsync(cancellationToken);

    public Task<DemoFleetStatusDto> StepSimulationAsync(CancellationToken cancellationToken = default) =>
        GetStatusAsync(cancellationToken);

    public Task<IReadOnlyCollection<DemoVehicleStateDto>> GetDemoVehiclesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<DemoVehicleStateDto>>([]);

    public Task<bool> ToggleVehicleOfflineSimulationAsync(Guid vehicleId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
