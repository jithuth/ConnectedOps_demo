using ConnectedOps.Application.Predictive;
using ConnectedOps.Domain.Predictive;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Predictive;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class Phase17PredictiveMaintenanceTests
{
    private static async Task<Vehicle> SeedVehicleAsync(
        ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "TRUCK-101",
        decimal odometer = 75000m)
    {
        var category = new VehicleCategory(tenantId, "Heavy Truck", "TRUCK", null, true);
        var make = new VehicleMake(tenantId, "Volvo", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "FH16", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            vehicleNumber,
            category.Id,
            make.Id,
            model.Id,
            displayName: "Volvo FH16 Longhaul",
            registrationNumber: "DXB-77665",
            currentOdometer: odometer,
            status: VehicleStatus.Active);
        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    [Fact]
    public async Task DiagnosticScan_EvaluatesSubsystemsAndGeneratesAlerts()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId, "TRUCK-901", odometer: 180000m);

        var service = new PredictiveMaintenanceService(db, userContext, NullLogger<PredictiveMaintenanceService>.Instance);

        // Run diagnostic scan
        var alertsGenerated = await service.RunFleetDiagnosticEvaluationAsync(vehicle.Id);

        // Check subsystem health records (5 subsystems)
        var healthRecords = await db.VehicleSubsystemHealths
            .Where(h => h.TenantId == tenantId && h.VehicleId == vehicle.Id)
            .ToListAsync();

        Assert.Equal(5, healthRecords.Count);
        Assert.Contains(healthRecords, h => h.Subsystem == SubsystemCategory.PowertrainEngine);
        Assert.Contains(healthRecords, h => h.Subsystem == SubsystemCategory.ElectricalBattery);
        Assert.Contains(healthRecords, h => h.Subsystem == SubsystemCategory.BrakingChassis);
        Assert.Contains(healthRecords, h => h.Subsystem == SubsystemCategory.TransmissionDrivetrain);
        Assert.Contains(healthRecords, h => h.Subsystem == SubsystemCategory.TiresSuspension);

        // For high mileage vehicle (180,000 km), some subsystems should degrade and produce alerts
        var alerts = await db.PredictiveMaintenanceAlerts
            .Where(a => a.TenantId == tenantId && a.VehicleId == vehicle.Id)
            .ToListAsync();

        Assert.NotEmpty(alerts);
        var firstAlert = alerts.First();
        Assert.Equal(PredictiveRecommendationStatus.Active, firstAlert.Status);
        Assert.True(firstAlert.FailureProbability > 0);
        Assert.NotEmpty(firstAlert.ComponentTitle);
        Assert.NotEmpty(firstAlert.RecommendedAction);
    }

    [Fact]
    public async Task GetVehicleDigitalTwin_ReturnsAllFiveSubsystemsAndOverallScore()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId, "VAN-301", odometer: 20000m);

        var service = new PredictiveMaintenanceService(db, userContext, NullLogger<PredictiveMaintenanceService>.Instance);

        var twin = await service.GetVehicleDigitalTwinAsync(vehicle.Id);

        Assert.NotNull(twin);
        Assert.Equal(vehicle.Id, twin.VehicleId);
        Assert.Equal("VAN-301", twin.VehicleNumber);
        Assert.Equal(5, twin.Subsystems.Count);
        Assert.True(twin.OverallHealthScore >= 70m); // Lower mileage vehicle should have high health
    }

    [Fact]
    public async Task FleetDashboard_ComputesAggregatesAndPotentialSavings()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        await SeedVehicleAsync(db, tenantId, "TRK-01", odometer: 150000m);
        await SeedVehicleAsync(db, tenantId, "TRK-02", odometer: 30000m);

        var service = new PredictiveMaintenanceService(db, userContext, NullLogger<PredictiveMaintenanceService>.Instance);
        await service.RunFleetDiagnosticEvaluationAsync();

        var dashboard = await service.GetFleetDashboardAsync();

        Assert.NotNull(dashboard);
        Assert.Equal(2, dashboard.TotalMonitoredVehicles);
        Assert.True(dashboard.AverageFleetHealthScore > 0 && dashboard.AverageFleetHealthScore <= 100);
        Assert.NotEmpty(dashboard.TopAtRiskVehicles);
        Assert.True(dashboard.PotentialSavingsEstimated >= 0);
    }

    [Fact]
    public async Task PromoteAlertToWorkOrder_CreatesMaintenanceRecordAndLinksIds()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId, "HAUL-55", odometer: 190000m);

        var service = new PredictiveMaintenanceService(db, userContext, NullLogger<PredictiveMaintenanceService>.Instance);
        await service.RunFleetDiagnosticEvaluationAsync(vehicle.Id);

        var alerts = await service.GetPredictiveAlertsPagedAsync(vehicle.Id);
        Assert.NotEmpty(alerts.Items);

        var targetAlert = alerts.Items.First();

        // Promote to Work Order
        var scheduledDate = DateTime.UtcNow.AddDays(2);
        var workOrderId = await service.PromoteAlertToWorkOrderAsync(
            targetAlert.Id,
            new PromoteToWorkOrderRequest("Priority maintenance before next delivery run", scheduledDate));

        Assert.NotEqual(Guid.Empty, workOrderId);

        // Verify alert updated status
        var updatedAlert = await db.PredictiveMaintenanceAlerts.FirstOrDefaultAsync(a => a.Id == targetAlert.Id);
        Assert.NotNull(updatedAlert);
        Assert.Equal(PredictiveRecommendationStatus.WorkOrderCreated, updatedAlert.Status);
        Assert.Equal(workOrderId, updatedAlert.PromotedMaintenanceRecordId);

        // Verify maintenance record created in database
        var maintRecord = await db.VehicleMaintenanceRecords.FirstOrDefaultAsync(m => m.Id == workOrderId);
        Assert.NotNull(maintRecord);
        Assert.Equal(vehicle.Id, maintRecord.VehicleId);
        Assert.Contains(targetAlert.ComponentTitle, maintRecord.Description);
    }

    [Fact]
    public async Task DismissAlert_UpdatesStatusAndReason()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId, "BUS-12", odometer: 180000m);

        var service = new PredictiveMaintenanceService(db, userContext, NullLogger<PredictiveMaintenanceService>.Instance);
        await service.RunFleetDiagnosticEvaluationAsync(vehicle.Id);

        var alerts = await service.GetPredictiveAlertsPagedAsync(vehicle.Id);
        Assert.NotEmpty(alerts.Items);

        var targetAlert = alerts.Items.First();
        var dismissed = await service.DismissAlertAsync(targetAlert.Id, "Vehicle scheduled for planned decommissioning");
        Assert.True(dismissed);

        var updated = await db.PredictiveMaintenanceAlerts.FirstOrDefaultAsync(a => a.Id == targetAlert.Id);
        Assert.NotNull(updated);
        Assert.Equal(PredictiveRecommendationStatus.Dismissed, updated.Status);
        Assert.Equal("Vehicle scheduled for planned decommissioning", updated.DismissReason);
    }

    [Fact]
    public async Task Phase17_TenantIsolation_PredictiveDataIsCompletelyIsolated()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userContextB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };

        var vehicleA = await SeedVehicleAsync(db, tenantA, "V-AAA", odometer: 170000m);
        var vehicleB = await SeedVehicleAsync(db, tenantB, "V-BBB", odometer: 170000m);

        var serviceA = new PredictiveMaintenanceService(db, userContextA, NullLogger<PredictiveMaintenanceService>.Instance);
        var serviceB = new PredictiveMaintenanceService(db, userContextB, NullLogger<PredictiveMaintenanceService>.Instance);

        await serviceA.RunFleetDiagnosticEvaluationAsync(vehicleA.Id);
        await serviceB.RunFleetDiagnosticEvaluationAsync(vehicleB.Id);

        var alertsA = await serviceA.GetPredictiveAlertsPagedAsync();
        var alertsB = await serviceB.GetPredictiveAlertsPagedAsync();

        // Tenant A should only see alerts for vehicle A
        Assert.All(alertsA.Items, a => Assert.Equal(vehicleA.Id, a.VehicleId));

        // Tenant B should only see alerts for vehicle B
        Assert.All(alertsB.Items, b => Assert.Equal(vehicleB.Id, b.VehicleId));

        // Tenant B cannot dismiss Tenant A's alert
        if (alertsA.Items.Count > 0)
        {
            var dismissFail = await serviceB.DismissAlertAsync(alertsA.Items.First().Id, "Unauthorized dismiss");
            Assert.False(dismissFail);
        }

        // Tenant B cannot retrieve Tenant A's digital twin
        var twinFromB = await serviceB.GetVehicleDigitalTwinAsync(vehicleA.Id);
        Assert.Null(twinFromB);
    }
}
