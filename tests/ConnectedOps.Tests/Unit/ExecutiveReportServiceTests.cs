using ConnectedOps.Application.Reports;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Reports;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Reports;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class ExecutiveReportServiceTests
{
    private static async Task<Vehicle> SeedVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "VH-001",
        FuelType fuelType = FuelType.Diesel,
        decimal acquisitionCost = 45000m,
        decimal odometer = 12000m)
    {
        var category = new VehicleCategory(tenantId, "Heavy Duty", "HD", "Heavy Duty Trucks", true);
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
            displayName: $"Truck {vehicleNumber}",
            internalCode: $"TCK-{vehicleNumber}",
            registrationNumber: $"REG-{vehicleNumber}",
            vin: $"1VOLVO{Guid.NewGuid():N}".Substring(0, 17).ToUpperInvariant(),
            chassisNumber: "CHS-12345",
            engineNumber: "ENG-67890",
            modelYear: 2023,
            manufactureYear: 2023,
            fuelType: fuelType,
            transmissionType: TransmissionType.Automatic,
            ownershipType: OwnershipType.CompanyOwned,
            branchId: null,
            locationId: null,
            currentOdometer: odometer,
            purchasePrice: acquisitionCost);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();

        return vehicle;
    }

    [Fact]
    public async Task ExecutiveReportService_ThrowsException_WhenTenantContextNotResolved()
    {
        using var db = TestDbContextFactory.Create();
        var userContext = new TestUserContext { TenantId = null, UserId = null };
        var service = new ExecutiveReportService(db, userContext);

        var filter = new ReportFilterParameters();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetExecutiveSummaryAsync(filter));
    }

    [Fact]
    public async Task ExecutiveReportService_EnforcesStrictTenantIsolation_TenantACannotSeeTenantB()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed vehicles for both tenants
        await SeedVehicleAsync(db, tenantA, "T-A-01", FuelType.Diesel, 50000m);
        await SeedVehicleAsync(db, tenantB, "T-B-01", FuelType.Diesel, 60000m);

        // Run as Tenant A
        var userContextA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var serviceA = new ExecutiveReportService(db, userContextA);

        var tcoA = await serviceA.GetFleetTcoReportAsync(new ReportFilterParameters());

        Assert.Single(tcoA.Vehicles);
        Assert.Equal("REG-T-A-01", tcoA.Vehicles[0].LicensePlate);
        Assert.Equal(50000m, tcoA.GrandTotalAcquisition);
    }

    [Fact]
    public async Task GetFleetTcoReportAsync_CalculatesAcquisitionFuelMaintenanceAndIncidentCostsAccurately()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ExecutiveReportService(db, userContext);

        var vehicle = await SeedVehicleAsync(db, tenantId, "FLT-TCO-1", FuelType.Diesel, 40000m, 10000m);

        // 1. Fuel transaction: 200L @ $300
        var station = new FuelStation(tenantId, "SHELL-01", "Shell Express", "Shell", FuelStationType.External);
        db.FuelStations.Add(station);
        await db.SaveChangesAsync();

        var fuelTx = new FuelTransaction(
            tenantId: tenantId,
            vehicleId: vehicle.Id,
            transactionDateUtc: DateTime.UtcNow.AddDays(-5),
            quantity: 200m,
            unitPrice: 1.50m,
            totalCost: 300m,
            fuelType: FuelType.Diesel,
            quantityUnit: FuelUnit.Liter,
            currencyCode: "USD",
            isFullTank: true,
            isPartialFill: false,
            odometerReading: 10200m,
            fuelStationId: station.Id);
        db.FuelTransactions.Add(fuelTx);

        // 2. Maintenance Record with Expense: $450 total cost
        var provider = new MaintenanceProvider(tenantId, "PRV-01", "Quick Lube", MaintenanceProviderType.ExternalWorkshop);
        var serviceType = new MaintenanceServiceType(tenantId, "OIL_CHANGE", "Oil Change", MaintenanceServiceCategory.Preventive, "Standard oil change");
        db.MaintenanceProviders.Add(provider);
        db.MaintenanceServiceTypes.Add(serviceType);
        await db.SaveChangesAsync();

        var maintRecord = new VehicleMaintenanceRecord(
            tenantId: tenantId,
            vehicleId: vehicle.Id,
            maintenanceServiceTypeId: serviceType.Id,
            serviceDateUtc: DateTime.UtcNow.AddDays(-3),
            maintenanceProviderId: provider.Id,
            odometerReading: 10300m,
            status: MaintenanceRecordStatus.Completed);
        db.VehicleMaintenanceRecords.Add(maintRecord);
        await db.SaveChangesAsync();

        var expense = new VehicleMaintenanceExpense(
            tenantId: tenantId,
            maintenanceRecordId: maintRecord.Id,
            expenseType: MaintenanceExpenseType.ExternalService,
            description: "Oil and filters",
            amount: 450m);
        db.VehicleMaintenanceExpenses.Add(expense);
        await db.SaveChangesAsync();

        maintRecord.RecalculateTotals();
        await db.SaveChangesAsync();

        // 3. Safety Incident: Moderate severity ($1000 estimate) linked to vehicle
        var incident = new SafetyIncident(
            tenantId: tenantId,
            incidentNumber: "INC-2026-001",
            incidentType: SafetyIncidentType.VehicleAccident,
            severity: SafetyIncidentSeverity.Moderate,
            occurredAtUtc: DateTime.UtcNow.AddDays(-2),
            title: "Fender bender",
            description: "Minor side bump");
        db.SafetyIncidents.Add(incident);
        await db.SaveChangesAsync();

        var incVehicle = new SafetyIncidentVehicle(
            tenantId: tenantId,
            safetyIncidentId: incident.Id,
            vehicleId: vehicle.Id,
            damageReported: true,
            isPrimaryVehicle: true);
        db.SafetyIncidentVehicles.Add(incVehicle);
        await db.SaveChangesAsync();

        // Query TCO Report
        var result = await service.GetFleetTcoReportAsync(new ReportFilterParameters());

        Assert.NotNull(result);
        Assert.Single(result.Vehicles);

        var item = result.Vehicles[0];
        Assert.Equal(40000m, item.AcquisitionPrice);
        Assert.Equal(300m, item.FuelCost);
        Assert.Equal(450m, item.MaintenanceCost);
        Assert.Equal(1000m, item.IncidentCost);
        Assert.Equal(1750m, item.TotalOperatingCost);
        Assert.Equal(41750m, item.TotalTco);

        Assert.Equal(40000m, result.GrandTotalAcquisition);
        Assert.Equal(300m, result.GrandTotalFuel);
        Assert.Equal(450m, result.GrandTotalMaintenance);
        Assert.Equal(1000m, result.GrandTotalIncidents);
        Assert.Equal(41750m, result.GrandTotalTco);
    }

    [Fact]
    public async Task GetEsgCarbonReportAsync_CalculatesScope1EmissionsAndEvSavingsAccurately()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ExecutiveReportService(db, userContext);

        var vehicle = await SeedVehicleAsync(db, tenantId, "FLT-ESG-1", FuelType.Diesel, 35000m, 5000m);

        var station = new FuelStation(tenantId, "ECO-01", "Eco Fuel", "Eco", FuelStationType.External);
        db.FuelStations.Add(station);
        await db.SaveChangesAsync();

        // 1000 Liters of Diesel -> 1000 * 2.68 kg CO2e = 2680 kg CO2e = 2.68 Metric Tons
        var fuelTx = new FuelTransaction(
            tenantId: tenantId,
            vehicleId: vehicle.Id,
            transactionDateUtc: DateTime.UtcNow.AddDays(-1),
            quantity: 1000m,
            unitPrice: 1.50m,
            totalCost: 1500m,
            fuelType: FuelType.Diesel,
            quantityUnit: FuelUnit.Liter,
            currencyCode: "USD",
            isFullTank: true,
            isPartialFill: false,
            odometerReading: 5000m,
            fuelStationId: station.Id);
        db.FuelTransactions.Add(fuelTx);
        await db.SaveChangesAsync();

        var esg = await service.GetEsgCarbonReportAsync(new ReportFilterParameters());

        Assert.NotNull(esg);
        Assert.Equal(2.68m, esg.TotalCo2MetricTons);
        Assert.Equal(2.68m, esg.DirectScope1FuelTons);
        Assert.NotEmpty(esg.FuelBreakdown);

        var dieselBreakdown = esg.FuelBreakdown.FirstOrDefault(x => string.Equals(x.FuelType, "Diesel", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(dieselBreakdown);
        Assert.Equal(1000m, dieselBreakdown.TotalVolume);
        Assert.Equal(2.68m, dieselBreakdown.Co2MetricTons);
        Assert.Equal(100m, dieselBreakdown.PercentageOfTotal);

        // Potential EV savings (approx 85% reduction)
        Assert.True(esg.PotentialEvCo2SavingsMetricTons > 0);
        Assert.True(esg.EstimatedEvFuelCostSavings > 0);
    }

    [Fact]
    public async Task GetFleetUtilizationReportAsync_CalculatesOperatingHoursAndDayOfWeekHeatmap()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ExecutiveReportService(db, userContext);

        var vehicle = await SeedVehicleAsync(db, tenantId, "UTIL-01", FuelType.Gasoline, 30000m, 8000m);

        var driver = new Driver(tenantId, "DRV-001", "John", "Doe", "+1234567890");
        db.Drivers.Add(driver);
        await db.SaveChangesAsync();

        // Add 1 usage session: 4 hours, 150 km
        var now = DateTime.UtcNow;
        var session1 = new VehicleUsageSession(
            tenantId: tenantId,
            vehicleId: vehicle.Id,
            driverId: driver.Id,
            startOdometer: 8000m,
            checkedOutAtUtc: now.AddHours(-10));

        session1.CheckIn(
            endOdometer: 8150m,
            endLocationId: null,
            condition: VehicleCondition.Good,
            checkedInByUserId: null,
            notes: "Checked in clean",
            checkedInAtUtc: now.AddHours(-6));

        db.VehicleUsageSessions.Add(session1);
        await db.SaveChangesAsync();

        var util = await service.GetFleetUtilizationReportAsync(new ReportFilterParameters());

        Assert.NotNull(util);
        Assert.Single(util.VehicleUtilizations);
        Assert.Equal(4.0, util.TotalFleetOperatingHours, 1);
        Assert.Equal(150m, util.TotalFleetDistanceKm);
        Assert.Equal(7, util.DayOfWeekHeatmap.Count); // 7 days of the week
    }

    [Fact]
    public async Task ExportReportCsvAsync_ProducesValidCsvFormattedOutput()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new ExecutiveReportService(db, userContext);

        await SeedVehicleAsync(db, tenantId, "EXP-01", FuelType.Diesel, 25000m, 5000m);

        var export = await service.ExportReportCsvAsync("tco", new ReportFilterParameters());

        Assert.NotNull(export);
        Assert.StartsWith("text/csv", export.ContentType);
        Assert.Contains("tco", export.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.True(export.FileBytes.Length > 0);

        var csvText = System.Text.Encoding.UTF8.GetString(export.FileBytes);
        Assert.Contains("License Plate,Make,Model", csvText);
        Assert.Contains("REG-EXP-01", csvText);
    }
}
