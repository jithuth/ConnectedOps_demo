using ConnectedOps.Application.Alerts;
using ConnectedOps.Application.ColdChain;
using ConnectedOps.Application.Inspections;
using ConnectedOps.Application.TollsAndFines;
using ConnectedOps.Domain.Alerts;
using ConnectedOps.Domain.ColdChain;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Inspections;
using ConnectedOps.Domain.TollsAndFines;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Alerts;
using ConnectedOps.Infrastructure.ColdChain;
using ConnectedOps.Infrastructure.Inspections;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.TollsAndFines;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class OperationalIntelligenceServiceTests
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
            registrationNumber: "DXB-A-1234",
            currentOdometer: 10000m,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);

        var driver = new Driver(
            tenantId,
            "DRV-001",
            "Rashid",
            "Al-Maktoum",
            "+971501234567",
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

    // =========================================================================
    // 1. ALERTS & NOTIFICATION ENGINE TESTS (WHATSAPP & TELEGRAM)
    // =========================================================================
    [Fact]
    public async Task AlertService_Trigger_Acknowledge_Resolve_Lifecycle()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new AlertService(db, context, NullLogger<AlertService>.Instance);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // 1. Create Alert Rule
        var rule = await service.CreateRuleAsync(new CreateAlertRuleRequest(
            Code: "HIGH_SPEED_130",
            Name: "Excessive Highway Speed",
            SourceType: AlertSourceType.Telemetry,
            Severity: AlertSeverity.Critical,
            CooldownMinutes: 10,
            Description: "Triggered when speed exceeds 130 km/h"));

        Assert.NotNull(rule);
        Assert.Equal("HIGH_SPEED_130", rule.Code);

        // 2. Add Triggered Alert
        var alert = new Alert(
            tenantId,
            title: "Excessive Highway Speed (142 km/h)",
            message: "Vehicle DXB-A-1234 exceeded speed limit by 22 km/h",
            severity: AlertSeverity.Critical,
            sourceType: AlertSourceType.Telemetry,
            alertRuleId: rule.Id,
            vehicleId: vehicle.Id,
            driverId: driver.Id,
            latitude: 25.2048,
            longitude: 55.2708);
        db.Alerts.Add(alert);
        await db.SaveChangesAsync();

        Assert.Equal(AlertStatus.Triggered, alert.Status);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);

        // 3. Acknowledge Alert
        var ackAlert = await service.AcknowledgeAlertAsync(alert.Id);
        Assert.Equal(AlertStatus.Acknowledged, ackAlert.Status);
        Assert.NotNull(ackAlert.AcknowledgedAtUtc);

        // 4. Resolve Alert
        var resolvedAlert = await service.ResolveAlertAsync(alert.Id, "Speed governor inspected and recalibrated");
        Assert.Equal(AlertStatus.Resolved, resolvedAlert.Status);
        Assert.NotNull(resolvedAlert.ResolvedAtUtc);
        Assert.Equal("Speed governor inspected and recalibrated", resolvedAlert.ResolutionNotes);
    }

    [Fact]
    public async Task AlertService_SendNotification_Supports_WhatsApp_And_Telegram()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new AlertService(db, context, NullLogger<AlertService>.Instance);

        // 1. Dispatch WhatsApp Notification
        var whatsAppResult = await service.SendNotificationAsync(new SendNotificationRequest(
            Channel: NotificationChannelType.WhatsApp,
            Recipient: "+971509998877",
            Title: "CRITICAL: Reefer Breach",
            Body: "Vehicle DXB-A-1234 compartment temp breached -14C. Check immediately."));

        Assert.NotNull(whatsAppResult);
        Assert.Equal(NotificationChannelType.WhatsApp, whatsAppResult.Channel);
        Assert.True(whatsAppResult.IsDelivered);
        Assert.StartsWith("WHATSAPP-", whatsAppResult.ExternalMessageId!);

        // 2. Dispatch Telegram Notification
        var telegramResult = await service.SendNotificationAsync(new SendNotificationRequest(
            Channel: NotificationChannelType.Telegram,
            Recipient: "@fleet_ops_emergency_bot",
            Title: "ALERT: Salik Gate Unmatched",
            Body: "Toll transaction at Al Barsha gate requires driver reconciliation."));

        Assert.NotNull(telegramResult);
        Assert.Equal(NotificationChannelType.Telegram, telegramResult.Channel);
        Assert.True(telegramResult.IsDelivered);
        Assert.StartsWith("TELEGRAM-", telegramResult.ExternalMessageId!);

        // 3. Check Dashboard Counts
        var dashboard = await service.GetDashboardAsync();
        Assert.True(dashboard.NotificationsSentTodayCount >= 2);
    }

    [Fact]
    public async Task AlertService_TenantIsolation_Enforced()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var serviceA = new AlertService(db, new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() }, NullLogger<AlertService>.Instance);
        var serviceB = new AlertService(db, new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() }, NullLogger<AlertService>.Instance);

        var ruleA = await serviceA.CreateRuleAsync(new CreateAlertRuleRequest(
            Code: "RULE_A",
            Name: "Rule Tenant A",
            SourceType: AlertSourceType.Telemetry,
            Severity: AlertSeverity.Warning));

        var rulesB = await serviceB.GetRulesAsync();
        Assert.DoesNotContain(rulesB, r => r.Id == ruleA.Id);
    }

    // =========================================================================
    // 2. DVIR INSPECTIONS & AUTOMATIC VEHICLE GROUNDING TESTS
    // =========================================================================
    [Fact]
    public async Task DvirService_PassedInspection_Maintains_Vehicle_Status()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DvirService(db, context);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        var request = new CreateDvirRequest(
            VehicleId: vehicle.Id,
            DriverId: driver.Id,
            InspectionType: DvirType.PreTrip,
            Odometer: 10500m,
            LocationName: "Jebel Ali Port Yard",
            DriverSignatureData: "SIG_VALID",
            Remarks: "Vehicle in perfect condition",
            Items: new List<CreateDvirItemCheckRequest>
            {
                new("Brakes", "Foot Brake", true),
                new("Tires", "Tread & Pressure", true)
            });

        var result = await service.CreateInspectionAsync(request);

        Assert.NotNull(result);
        Assert.Equal(DvirStatus.Passed, result.Status);

        var dbVehicle = await db.Vehicles.FindAsync(vehicle.Id);
        Assert.Equal(VehicleStatus.Active, dbVehicle!.Status);
    }

    [Fact]
    public async Task DvirService_CriticalDefect_AutomaticallyGroundsVehicle_And_MechanicSignOff_RestoresIt()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new DvirService(db, context);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // 1. Submit DVIR with CRITICAL Defect
        var request = new CreateDvirRequest(
            VehicleId: vehicle.Id,
            DriverId: driver.Id,
            InspectionType: DvirType.PreTrip,
            Odometer: 10500m,
            LocationName: "Dubai Depot",
            DriverSignatureData: "SIG_VALID",
            Remarks: "Brake fluid leaking heavily",
            Items: new List<CreateDvirItemCheckRequest>
            {
                new("Brakes", "Hydraulic Brake Lines", false, Severity: DefectSeverity.Critical, DefectDescription: "Severe hydraulic fluid leak, soft pedal"),
                new("Tires", "Tread & Pressure", true)
            });

        var inspection = await service.CreateInspectionAsync(request);

        Assert.Equal(DvirStatus.VehicleGrounded, inspection.Status);

        // Verify vehicle was grounded in DB (Status set to UnderMaintenance)
        var dbVehicle = await db.Vehicles.FindAsync(vehicle.Id);
        Assert.Equal(VehicleStatus.UnderMaintenance, dbVehicle!.Status);

        // 2. Mechanic Signs Off after repair
        var certified = await service.CertifyByMechanicAsync(
            inspection.Id,
            new SignOffDvirRequest(
                MechanicName: "Master Tech Hassan",
                MechanicNotes: "Replaced brake line hoses, flushed fluid and pressure tested 100% OK",
                MechanicSignatureData: "SIG_MECH_APPROVED"));

        Assert.Equal(DvirStatus.CertifiedSafe, certified.Status);
        Assert.Equal("Master Tech Hassan", certified.MechanicName);

        // Verify vehicle was restored back to InService
        var restoredVehicle = await db.Vehicles.FindAsync(vehicle.Id);
        Assert.Equal(VehicleStatus.InService, restoredVehicle!.Status);
    }

    // =========================================================================
    // 3. TOLLS & TRAFFIC VIOLATIONS AUTO-MATCHING & LIABILITY TESTS
    // =========================================================================
    [Fact]
    public async Task TollAndFineService_Ingestion_And_DriverAutoMatch()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new TollAndFineService(db, context);

        var (vehicle, driver) = await SeedVehicleAndDriverAsync(db, tenantId);

        // 1. Create a VehicleUsageSession covering the violation time
        var tripStart = DateTime.UtcNow.AddHours(-3);
        var session = new VehicleUsageSession(
            tenantId,
            vehicle.Id,
            driver.Id,
            startOdometer: 10000m,
            checkedOutAtUtc: tripStart);
        db.VehicleUsageSessions.Add(session);
        await db.SaveChangesAsync();

        // 2. Ingest Toll during the session
        var tollTime = DateTime.UtcNow.AddHours(-2);
        var toll = await service.CreateTollAsync(new CreateTollTransactionRequest(
            TollSystem: TollSystemType.Salik,
            TollGateName: "Al Barsha Toll Gate",
            TollGateCode: "SLK-DXB-04",
            Amount: 4.00m,
            TransactionTimeUtc: tollTime,
            VehicleId: vehicle.Id));

        Assert.Equal(driver.Id, toll.MatchedDriverId);
        Assert.Equal(session.Id, toll.MatchedUsageSessionId);

        // 3. Ingest Traffic Violation during the session
        var violationTime = DateTime.UtcNow.AddHours(-1);
        var violation = await service.CreateViolationAsync(new CreateTrafficViolationRequest(
            TicketNumber: "TKT-DXB-2026-001",
            AuthorityName: "Dubai Police",
            ViolationCode: "SPD-120",
            Description: "Exceeding speed limit by 20 km/h",
            FineAmount: 600m,
            BlackPoints: 2,
            ViolationTimeUtc: violationTime,
            VehicleId: vehicle.Id,
            Location: "Sheikh Zayed Road"));

        Assert.Equal(driver.Id, violation.MatchedDriverId);
        Assert.Equal(ViolationLiabilityStatus.AssignedToDriver, violation.LiabilityStatus);

        // 4. Dispute & Settle workflows
        var disputed = await service.DisputeViolationAsync(violation.Id, new DisputeViolationRequest("Emergency dispatch priority lane"));
        Assert.Equal(ViolationLiabilityStatus.Disputed, disputed.LiabilityStatus);

        var settled = await service.SettleViolationAsync(violation.Id, new SettleViolationRequest(ViolationLiabilityStatus.PayrollDeducted, "Deducted from driver payroll March 2026"));
        Assert.Equal(ViolationLiabilityStatus.PayrollDeducted, settled.LiabilityStatus);
    }

    // =========================================================================
    // 4. COLD CHAIN, REEFER & HACCP EXCURSION TESTS
    // =========================================================================
    [Fact]
    public async Task ColdChainService_Sensor_Telemetry_Excursion_Lifecycle()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var context = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ColdChainService(db, context);

        var (vehicle, _) = await SeedVehicleAndDriverAsync(db, tenantId);

        // 1. Register Cold Chain Sensor
        var sensor = await service.RegisterSensorAsync(new RegisterCargoSensorRequest(
            SensorTagNumber: "BLE-REEFER-01",
            CompartmentName: "Frozen Seafood Compartment",
            VehicleId: vehicle.Id,
            MinTargetTemperatureCelsius: -22.0,
            MaxTargetTemperatureCelsius: -18.0,
            BatteryLevelPercent: 95));

        Assert.NotNull(sensor);
        Assert.Equal("BLE-REEFER-01", sensor.SensorTagNumber);

        // 2. Record Normal Telemetry Reading (Within Range)
        var normalReading = await service.RecordTelemetryAsync(new RecordCargoTelemetryRequest(
            CargoSensorDeviceId: sensor.Id,
            RecordedAtUtc: DateTime.UtcNow.AddMinutes(-30),
            TemperatureCelsius: -19.8,
            HumidityPercent: 70,
            DoorOpen: false,
            ReeferMode: ReeferMode.Freezing,
            SetpointTemperatureCelsius: -20.0));

        Assert.Equal(-19.8, normalReading.TemperatureCelsius);

        var dashboardNormal = await service.GetDashboardAsync();
        Assert.Equal(0, dashboardNormal.ActiveExcursionsCount);
        Assert.Equal(1, dashboardNormal.CompartmentsInToleranceCount);

        // 3. Record Breach Telemetry (Temperature rises to -10.5C -> >7C breach above -18C max)
        var breachReading = await service.RecordTelemetryAsync(new RecordCargoTelemetryRequest(
            CargoSensorDeviceId: sensor.Id,
            RecordedAtUtc: DateTime.UtcNow,
            TemperatureCelsius: -10.5,
            HumidityPercent: 88,
            DoorOpen: true,
            ReeferMode: ReeferMode.Defrost));

        Assert.Equal(-10.5, breachReading.TemperatureCelsius);

        // 4. Verify HACCP Excursion Incident Triggered
        var dashboardBreach = await service.GetDashboardAsync();
        Assert.Equal(1, dashboardBreach.ActiveExcursionsCount);
        Assert.Equal(1, dashboardBreach.CriticalExcursionsCount);

        var excursionsPaged = await service.GetExcursionsPagedAsync(new ExcursionFilterRequest());
        Assert.Single(excursionsPaged.Items);
        var activeExcursion = excursionsPaged.Items.First();
        Assert.Equal(ExcursionSeverity.HaccpBreach, activeExcursion.Severity);
        Assert.Equal(ExcursionStatus.Active, activeExcursion.Status);

        // 5. Resolve Excursion Incident
        var resolved = await service.ResolveExcursionAsync(activeExcursion.Id, new ResolveExcursionRequest(
            ActionTaken: "Reefer door closed, compressor reset to continuous freezing, cargo temperature verified"));

        Assert.Equal(ExcursionStatus.Resolved, resolved.Status);
        Assert.NotNull(resolved.ResolvedAtUtc);
    }
}
