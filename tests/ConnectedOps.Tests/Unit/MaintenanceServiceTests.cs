using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Maintenance;
using ConnectedOps.Infrastructure.Vehicles;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class MaintenanceServiceTests
{
    private static async Task<(Guid CategoryId, Guid MakeId, Guid ModelId, Guid VehicleId)> SeedVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "FLT-MNT-01",
        decimal initialOdometer = 50000m,
        OdometerUnit unit = OdometerUnit.Kilometers)
    {
        var category = new VehicleCategory(tenantId, "Heavy Duty Truck", "HDT", "Trucks", true);
        var make = new VehicleMake(tenantId, "Volvo", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "FH16", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId: tenantId,
            vehicleNumber: vehicleNumber,
            categoryId: category.Id,
            makeId: make.Id,
            modelId: model.Id,
            displayName: $"Volvo FH16 #{vehicleNumber}",
            internalCode: $"TRK-{vehicleNumber}",
            registrationNumber: $"REG-{vehicleNumber}",
            vin: $"VIN{Guid.NewGuid():N}"[..17].ToUpperInvariant(),
            chassisNumber: "CHS-001",
            engineNumber: "ENG-001",
            modelYear: 2024,
            manufactureYear: 2024,
            fuelType: FuelType.Diesel,
            transmissionType: TransmissionType.Automatic,
            ownershipType: OwnershipType.CompanyOwned,
            branchId: null,
            locationId: null,
            currentOdometer: initialOdometer,
            odometerUnit: unit,
            status: VehicleStatus.InService);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return (category.Id, make.Id, model.Id, vehicle.Id);
    }

    [Fact]
    public async Task ServiceType_Create_Update_Delete_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var serviceA = new MaintenanceServiceTypeService(db, userContextA, audit);
        var serviceB = new MaintenanceServiceTypeService(db, userContextB, audit);

        // Create in Tenant A
        var createReq = new CreateMaintenanceServiceTypeRequest(
            Code: "OIL-CHG-10K",
            Name: "10,000 km Oil Change",
            Category: MaintenanceServiceCategory.Lubrication,
            Description: "Synthetic engine oil and filter change",
            DefaultDurationHours: 1.5m);

        var created = await serviceA.CreateAsync(createReq);
        Assert.NotNull(created);
        Assert.Equal("OIL-CHG-10K", created.Code);
        Assert.Equal(tenantA, created.TenantId);

        // Tenant B cannot see or modify Tenant A's service type
        var listB = await serviceB.GetAllAsync();
        Assert.DoesNotContain(listB, x => x.Id == created.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            serviceB.GetByIdAsync(created.Id));

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            serviceB.UpdateAsync(created.Id, new UpdateMaintenanceServiceTypeRequest(
                "OIL-CHG-MOD", "Hacked", MaintenanceServiceCategory.Other, null, null, true)));

        // Tenant A can update
        var updated = await serviceA.UpdateAsync(created.Id, new UpdateMaintenanceServiceTypeRequest(
            "OIL-CHG-10K", "10,000 km Oil & Filter Change", MaintenanceServiceCategory.Lubrication, "Updated desc", 2.0m, true));
        Assert.Equal(2.0m, updated.DefaultDurationHours);

        // Tenant A can delete
        await serviceA.DeleteAsync(created.Id);
        var listAfterDelete = await serviceA.GetAllAsync();
        Assert.DoesNotContain(listAfterDelete, x => x.Id == created.Id);
    }

    [Fact]
    public async Task Provider_Create_Update_Delete_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var serviceA = new MaintenanceProviderService(db, userContextA, audit);
        var serviceB = new MaintenanceProviderService(db, userContextB, audit);

        var created = await serviceA.CreateAsync(new CreateMaintenanceProviderRequest(
            Code: "WS-HQ",
            Name: "HQ Central Workshop",
            ProviderType: MaintenanceProviderType.InternalWorkshop,
            ContactPerson: "Chief Mechanic",
            Phone: "+1 555-1234",
            Email: "mechanic@connectedops.io"));

        Assert.NotNull(created);
        Assert.Equal(tenantA, created.TenantId);

        // Cross-tenant isolation
        await Assert.ThrowsAsync<KeyNotFoundException>(() => serviceB.GetByIdAsync(created.Id));

        // Update
        var updated = await serviceA.UpdateAsync(created.Id, new UpdateMaintenanceProviderRequest(
            Code: "WS-HQ-MAIN",
            Name: "HQ Primary Workshop",
            ProviderType: MaintenanceProviderType.InternalWorkshop,
            ContactPerson: "Chief Mechanic",
            Phone: "+1 555-9999",
            Email: "mechanic@connectedops.io"));
        Assert.Equal("WS-HQ-MAIN", updated.Code);

        // Delete
        await serviceA.DeleteAsync(created.Id);
        var list = await serviceA.GetAllAsync();
        Assert.DoesNotContain(list, x => x.Id == created.Id);
    }

    [Fact]
    public async Task MaintenancePlan_Create_AddRule_AssignVehicle_PreventDuplicates()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var planService = new MaintenancePlanService(db, userContext, audit);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "BRK-INSP", "Brake Pad Inspection", MaintenanceServiceCategory.Brake));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-001", 60000m);

        // 1. Create Plan
        var plan = await planService.CreateAsync(new CreateMaintenancePlanRequest(
            Code: "TRUCK-PM-STD",
            Name: "Heavy Truck Standard PM Schedule",
            Description: "Standard preventive schedule for heavy trucks"));
        Assert.NotNull(plan);

        // 2. Add Rule
        var rule = await planService.AddRuleAsync(plan.Id, new CreateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.Distance,
            IntervalKilometers: 20000m,
            ReminderBeforeKilometers: 1000m,
            ToleranceKilometers: 500m));
        Assert.NotNull(rule);
        Assert.Equal(20000m, rule.IntervalKilometers);

        // 3. Assign to Vehicle
        var assignment = await planService.AssignPlanToVehicleAsync(new AssignVehiclePlanRequest(
            VehicleId: vehicleId,
            MaintenancePlanId: plan.Id,
            EffectiveFromUtc: DateTime.UtcNow.AddDays(-30),
            BaselineOdometer: 60000m));
        Assert.NotNull(assignment);
        Assert.Equal(vehicleId, assignment.VehicleId);
        Assert.Equal(plan.Id, assignment.MaintenancePlanId);

        // 4. Duplicate Active Assignment should throw ConflictException
        await Assert.ThrowsAsync<ConflictException>(() =>
            planService.AssignPlanToVehicleAsync(new AssignVehiclePlanRequest(
                VehicleId: vehicleId,
                MaintenancePlanId: plan.Id,
                EffectiveFromUtc: DateTime.UtcNow)));
    }

    [Fact]
    public async Task ScheduleEvaluation_DistanceInterval_Upcoming_Due_Overdue()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var planService = new MaintenancePlanService(db, userContext, audit);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "FLUID-CHG", "Transmission Fluid Change", MaintenanceServiceCategory.Transmission));

        var plan = await planService.CreateAsync(new CreateMaintenancePlanRequest(
            Code: "TRANS-PLAN", Name: "Transmission Plan"));

        // Rule: 10,000 km interval, 1,000 km reminder window, 500 km tolerance
        await planService.AddRuleAsync(plan.Id, new CreateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.Distance,
            IntervalKilometers: 10000m,
            ReminderBeforeKilometers: 1000m,
            ToleranceKilometers: 500m));

        // Seed Vehicle at 50,000 km baseline
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-TEST-DIST", 50000m);

        await planService.AssignPlanToVehicleAsync(new AssignVehiclePlanRequest(
            VehicleId: vehicleId,
            MaintenancePlanId: plan.Id,
            EffectiveFromUtc: DateTime.UtcNow.AddMonths(-1),
            BaselineOdometer: 50000m));

        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicle);

        // Case 1: Current odometer = 58,000 km (Remaining = 2,000 km > 1,000 km reminder) -> NotDue
        vehicle.UpdateOdometer(58000m);
        await db.SaveChangesAsync();

        var dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        var item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.NotDue, item.DueStatus);
        Assert.Equal(60000m, item.NextDueOdometer);
        Assert.Equal(2000m, item.RemainingDistance);

        // Case 2: Current odometer = 59,200 km (Remaining = 800 km <= 1,000 km reminder) -> Upcoming
        vehicle.UpdateOdometer(59200m);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Upcoming, item.DueStatus);
        Assert.Equal(800m, item.RemainingDistance);

        // Case 3: Current odometer = 60,100 km (Passed 60,000 km but <= 60,500 km tolerance) -> Due
        vehicle.UpdateOdometer(60100m);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Due, item.DueStatus);
        Assert.Equal(-100m, item.RemainingDistance);

        // Case 4: Current odometer = 60,600 km (Passed 60,500 km tolerance limit) -> Overdue
        vehicle.UpdateOdometer(60600m);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Overdue, item.DueStatus);
        Assert.Equal(-600m, item.RemainingDistance);
    }

    [Fact]
    public async Task ScheduleEvaluation_CalendarInterval_Upcoming_Due_Overdue()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var planService = new MaintenancePlanService(db, userContext, audit);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "ANNUAL-INSP", "Annual Safety Inspection", MaintenanceServiceCategory.Safety));

        var plan = await planService.CreateAsync(new CreateMaintenancePlanRequest(
            Code: "CAL-PLAN", Name: "Annual Calendar Plan"));

        // Rule: 180 days interval, 14 days reminder window, 7 days tolerance
        await planService.AddRuleAsync(plan.Id, new CreateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.Calendar,
            IntervalDays: 180,
            ReminderBeforeDays: 14,
            ToleranceDays: 7));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-CAL-01", 10000m);

        // Effective 100 days ago -> remaining days ~80 -> NotDue
        var assignment = new VehicleMaintenancePlanAssignment(
            tenantId, vehicleId, plan.Id, DateTime.UtcNow.AddDays(-100), null, 10000m, null, userContext.UserId, null, true);
        db.VehicleMaintenancePlanAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        var item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.NotDue, item.DueStatus);

        // Effective 170 days ago -> remaining days ~10 <= 14 -> Upcoming
        assignment.Update(DateTime.UtcNow.AddDays(-170), null, 10000m, null, null, true);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Upcoming, item.DueStatus);

        // Effective 182 days ago -> overdue window has 7 days tolerance -> Due
        assignment.Update(DateTime.UtcNow.AddDays(-182), null, 10000m, null, null, true);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Due, item.DueStatus);

        // Effective 195 days ago -> passed 180 + 7 = 187 days -> Overdue
        assignment.Update(DateTime.UtcNow.AddDays(-195), null, 10000m, null, null, true);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Overdue, item.DueStatus);
    }

    [Fact]
    public async Task ScheduleEvaluation_CombinedRules_DistanceOrCalendar_DistanceAndCalendar()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var planService = new MaintenancePlanService(db, userContext, audit);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "STD-SVC", "Standard Service A", MaintenanceServiceCategory.Preventive));

        var plan = await planService.CreateAsync(new CreateMaintenancePlanRequest(
            Code: "COMBINED-PLAN", Name: "Combined Schedule Plan"));

        // Rule 1: DistanceOrCalendar (10,000 km or 180 days)
        var ruleOr = await planService.AddRuleAsync(plan.Id, new CreateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.DistanceOrCalendar,
            IntervalKilometers: 10000m,
            IntervalDays: 180,
            ReminderBeforeKilometers: 1000m,
            ReminderBeforeDays: 14,
            ToleranceKilometers: 500m,
            ToleranceDays: 7));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-COMBINED-01", 50000m);

        // Effective 30 days ago (Calendar is NotDue), but vehicle drove 10,800 km (Distance is Overdue)
        var assignment = new VehicleMaintenancePlanAssignment(
            tenantId, vehicleId, plan.Id, DateTime.UtcNow.AddDays(-30), null, 50000m, null, userContext.UserId, null, true);
        db.VehicleMaintenancePlanAssignments.Add(assignment);

        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicle);
        vehicle.UpdateOdometer(60800m);
        await db.SaveChangesAsync();

        var dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        var item = Assert.Single(dueList);
        // DistanceOrCalendar takes the most severe status (Overdue > Due > Upcoming > NotDue)
        Assert.Equal(MaintenanceDueStatus.Overdue, item.DueStatus);

        // Rule 2: DistanceAndCalendar
        await planService.UpdateRuleAsync(plan.Id, ruleOr.Id, new UpdateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.DistanceAndCalendar,
            IntervalKilometers: 10000m,
            IntervalDays: 180,
            ReminderBeforeKilometers: 1000m,
            ReminderBeforeDays: 14,
            ToleranceKilometers: 500m,
            ToleranceDays: 7));

        // For DistanceAndCalendar: Distance is Overdue, but Calendar is NotDue (only 30 days passed). Overall status is NotDue because both must be triggered!
        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.NotDue, item.DueStatus);
    }

    [Fact]
    public async Task ScheduleEvaluation_UnitConversion_MilesVehicle_KilometersRule()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var planService = new MaintenancePlanService(db, userContext, audit);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "TIRE-ROT", "Tire Rotation", MaintenanceServiceCategory.Tyre));

        var plan = await planService.CreateAsync(new CreateMaintenancePlanRequest(
            Code: "TIRE-PLAN", Name: "Tire Maintenance Plan"));

        // Rule specified in Kilometers: 10,000 km (~6,213.71 miles), Reminder: 1,000 km (~621 miles), Tolerance: 500 km (~310 miles)
        await planService.AddRuleAsync(plan.Id, new CreateMaintenancePlanRuleRequest(
            MaintenanceServiceTypeId: st.Id,
            ScheduleType: MaintenanceScheduleType.Distance,
            IntervalKilometers: 10000m,
            ReminderBeforeKilometers: 1000m,
            ToleranceKilometers: 500m));

        // Vehicle in Miles: initial odometer 10,000 miles (~16,093.44 km)
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-MILES-01", 10000m, OdometerUnit.Miles);

        await planService.AssignPlanToVehicleAsync(new AssignVehiclePlanRequest(
            VehicleId: vehicleId,
            MaintenancePlanId: plan.Id,
            EffectiveFromUtc: DateTime.UtcNow.AddMonths(-1),
            BaselineOdometer: 10000m));

        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicle);

        // Baseline = 10,000 miles (16,093.44 km). Next due = 26,093.44 km = ~16,213.71 miles.
        // Current = 15,000 miles (24,140.16 km). Remaining km = 1,953.28 km > 1,000 km reminder -> NotDue
        vehicle.UpdateOdometer(15000m);
        await db.SaveChangesAsync();

        var dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        var item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.NotDue, item.DueStatus);

        // Current = 15,800 miles (25,427.64 km). Remaining km = 665.80 km <= 1,000 km reminder -> Upcoming
        vehicle.UpdateOdometer(15800m);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Upcoming, item.DueStatus);

        // Current = 16,300 miles (26,232.31 km). Passed 26,093.44 km but <= 26,593.44 km tolerance -> Due
        vehicle.UpdateOdometer(16300m);
        await db.SaveChangesAsync();

        dueList = await scheduleService.CalculateVehicleMaintenanceAsync(vehicleId);
        item = Assert.Single(dueList);
        Assert.Equal(MaintenanceDueStatus.Due, item.DueStatus);
    }

    [Fact]
    public async Task ServiceRecord_CompleteService_UpdatesOdometer_TransitionsVehicleStatus_ClosesDowntime_CalculatesCosts()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var vehicleOdometerService = new VehicleOdometerService(db, userContext, audit);
        var vehicleService = new VehicleService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);
        var dueEvaluationService = new MaintenanceDueEvaluationService(db, scheduleService);
        var recordService = new MaintenanceRecordService(db, userContext, audit, vehicleOdometerService, vehicleService, dueEvaluationService);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var providerService = new MaintenanceProviderService(db, userContext, audit);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "MAJ-SVC", "Major Service 60K", MaintenanceServiceCategory.Preventive));

        var provider = await providerService.CreateAsync(new CreateMaintenanceProviderRequest(
            "WS-01", "Fleet Workshop 1", MaintenanceProviderType.InternalWorkshop));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-SVC-01", 60000m);

        // 1. Create Maintenance Record
        var record = await recordService.CreateAsync(new CreateMaintenanceRecordRequest(
            VehicleId: vehicleId,
            MaintenanceServiceTypeId: st.Id,
            ServiceDateUtc: DateTime.UtcNow,
            MaintenanceProviderId: provider.Id,
            OdometerReading: 60000m,
            MaintenanceType: VehicleMaintenanceType.Preventive,
            Description: "60,000 km major service overhaul"));

        Assert.NotNull(record);
        Assert.Equal(MaintenanceRecordStatus.Draft, record.Status);

        // 2. Add Parts, Labour, Expenses
        await recordService.AddPartAsync(record.Id, new AddMaintenancePartRequest(
            PartName: "Engine Oil Synthetic 5W30 (5L)",
            Quantity: 2m,
            UnitCost: 45.00m)); // $90.00

        await recordService.AddPartAsync(record.Id, new AddMaintenancePartRequest(
            PartName: "Oil Filter Element",
            Quantity: 1m,
            UnitCost: 25.00m)); // $25.00

        await recordService.AddLabourAsync(record.Id, new AddMaintenanceLabourRequest(
            Description: "General Service Technician",
            Hours: 3.5m,
            HourlyRate: 80.00m,
            TechnicianName: "Alex Smith")); // $280.00

        await recordService.AddExpenseAsync(record.Id, new AddMaintenanceExpenseRequest(
            ExpenseType: MaintenanceExpenseType.InspectionFee,
            Description: "Emissions testing bench fee",
            Amount: 50.00m)); // $50.00

        // 3. Start Service -> Vehicle transitions to UnderMaintenance & creates active downtime
        var startedRecord = await recordService.StartServiceAsync(record.Id);
        Assert.Equal(MaintenanceRecordStatus.InProgress, startedRecord.Status);

        var vehicleDuring = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicleDuring);
        Assert.Equal(VehicleStatus.UnderMaintenance, vehicleDuring.Status);

        var downtimeActive = db.VehicleDowntimeRecords.FirstOrDefault(d => d.MaintenanceRecordId == record.Id);
        Assert.NotNull(downtimeActive);
        Assert.Null(downtimeActive.EndedAtUtc);

        // 4. Complete Service
        var completedRecord = await recordService.CompleteServiceAsync(record.Id, new CompleteMaintenanceRecordRequest(
            CompletedDateTimeUtc: DateTime.UtcNow,
            FinalOdometer: 60050m,
            Notes: "Service completed successfully. Vehicle tested and roadworthy."));

        Assert.Equal(MaintenanceRecordStatus.Completed, completedRecord.Status);

        // Verify Server-Side Cost Calculation: $90 + $25 + $280 + $50 = $445.00
        Assert.Equal(115.00m, completedRecord.TotalPartsCost);
        Assert.Equal(280.00m, completedRecord.TotalLabourCost);
        Assert.Equal(50.00m, completedRecord.OtherCost);
        Assert.Equal(445.00m, completedRecord.TotalCost);

        // Verify Vehicle Status transitioned back to InService
        var vehicleAfter = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicleAfter);
        Assert.Equal(VehicleStatus.InService, vehicleAfter.Status);
        Assert.Equal(60050m, vehicleAfter.CurrentOdometer);

        // Verify Odometer Entry was recorded via Phase 3 IVehicleOdometerService
        var odoLogs = db.VehicleOdometerEntries.Where(o => o.VehicleId == vehicleId).ToList();
        Assert.Contains(odoLogs, o => o.Reading == 60050m && o.Source == OdometerSource.Maintenance);

        // Verify Downtime Record was closed
        var downtimeClosed = db.VehicleDowntimeRecords.FirstOrDefault(d => d.MaintenanceRecordId == record.Id);
        Assert.NotNull(downtimeClosed);
        Assert.NotNull(downtimeClosed.EndedAtUtc);
    }

    [Fact]
    public async Task ServiceRecord_CancelService_RevertsVehicleStatus_ClosesDowntime()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var vehicleOdometerService = new VehicleOdometerService(db, userContext, audit);
        var vehicleService = new VehicleService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);
        var dueEvaluationService = new MaintenanceDueEvaluationService(db, scheduleService);
        var recordService = new MaintenanceRecordService(db, userContext, audit, vehicleOdometerService, vehicleService, dueEvaluationService);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "INSP-01", "Periodic Inspection", MaintenanceServiceCategory.Inspection));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-CANC-01", 30000m);

        var record = await recordService.CreateAsync(new CreateMaintenanceRecordRequest(
            VehicleId: vehicleId,
            MaintenanceServiceTypeId: st.Id,
            ServiceDateUtc: DateTime.UtcNow));

        // Start service
        await recordService.StartServiceAsync(record.Id);
        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        Assert.NotNull(vehicle);
        Assert.Equal(VehicleStatus.UnderMaintenance, vehicle.Status);

        // Cancel service
        var cancelled = await recordService.CancelServiceAsync(record.Id, new CancelMaintenanceRecordRequest("Cancelled by fleet supervisor"));
        Assert.Equal(MaintenanceRecordStatus.Cancelled, cancelled.Status);

        // Vehicle status restored to InService
        await db.Entry(vehicle).ReloadAsync();
        Assert.Equal(VehicleStatus.InService, vehicle.Status);

        // Downtime closed
        var downtime = db.VehicleDowntimeRecords.FirstOrDefault(d => d.MaintenanceRecordId == record.Id);
        Assert.NotNull(downtime);
        Assert.NotNull(downtime.EndedAtUtc);
    }

    [Fact]
    public async Task MaintenanceDashboardService_AggregatesAccurateMetrics()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var vehicleOdometerService = new VehicleOdometerService(db, userContext, audit);
        var vehicleService = new VehicleService(db, userContext, audit);
        var engineHoursProvider = new VehicleEngineHoursProvider(db);
        var scheduleService = new MaintenanceScheduleService(db, userContext, engineHoursProvider);
        var dueEvaluationService = new MaintenanceDueEvaluationService(db, scheduleService);
        var recordService = new MaintenanceRecordService(db, userContext, audit, vehicleOdometerService, vehicleService, dueEvaluationService);
        var stService = new MaintenanceServiceTypeService(db, userContext, audit);
        var dashboardService = new MaintenanceDashboardService(db, userContext, scheduleService);

        var st = await stService.CreateAsync(new CreateMaintenanceServiceTypeRequest(
            "GEN-SVC", "General Service", MaintenanceServiceCategory.Preventive));

        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, "FLT-DASH-01", 40000m);

        // Create and complete a record
        var record = await recordService.CreateAsync(new CreateMaintenanceRecordRequest(
            VehicleId: vehicleId,
            MaintenanceServiceTypeId: st.Id,
            ServiceDateUtc: DateTime.UtcNow));

        await recordService.AddLabourAsync(record.Id, new AddMaintenanceLabourRequest("Mechanic Work", 2m, 100m));
        await recordService.StartServiceAsync(record.Id);
        await recordService.CompleteServiceAsync(record.Id, new CompleteMaintenanceRecordRequest(DateTime.UtcNow, 40100m));

        var summary = await dashboardService.GetDashboardMetricsAsync();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.ServicesCompletedToday);
        Assert.Equal(1, summary.ServicesCompletedThisMonth);
        Assert.Equal(200.00m, summary.MaintenanceCostThisMonth);
    }
}
