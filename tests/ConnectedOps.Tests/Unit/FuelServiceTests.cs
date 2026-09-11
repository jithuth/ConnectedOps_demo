using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Fuel;
using ConnectedOps.Infrastructure.Vehicles;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class FuelServiceTests
{
    private static async Task<(Guid CategoryId, Guid MakeId, Guid ModelId, Guid VehicleId)> SeedVehicleAsync(
        ConnectedOps.Infrastructure.Persistence.ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "FLT-FUEL-01",
        decimal initialOdometer = 50000m,
        decimal? tankCapacity = 300m,
        OdometerUnit unit = OdometerUnit.Kilometers)
    {
        var category = new VehicleCategory(tenantId, "Heavy Duty Hauler", "HDH", "Trucks", true);
        var make = new VehicleMake(tenantId, "Scania", "SE");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "R500", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId: tenantId,
            vehicleNumber: vehicleNumber,
            categoryId: category.Id,
            makeId: make.Id,
            modelId: model.Id,
            displayName: $"Scania R500 #{vehicleNumber}",
            internalCode: $"TRK-{vehicleNumber}",
            registrationNumber: $"REG-{vehicleNumber}",
            vin: $"VIN{Guid.NewGuid():N}"[..17].ToUpperInvariant(),
            chassisNumber: "CHS-FUEL-001",
            engineNumber: "ENG-FUEL-001",
            modelYear: 2025,
            manufactureYear: 2025,
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

        if (tankCapacity.HasValue)
        {
            var spec = new VehicleSpecification(
                tenantId: tenantId,
                vehicleId: vehicle.Id,
                engineCapacityCc: 13000,
                enginePowerKw: 370,
                cylinderCount: 6,
                fuelTankCapacity: tankCapacity.Value,
                batteryVoltage: 24,
                lengthMm: 6200,
                widthMm: 2550,
                heightMm: 3950,
                grossVehicleWeightKg: 26000,
                kerbWeightKg: 8500,
                payloadCapacityKg: 17500,
                axleCount: 3,
                wheelCount: 6,
                seatCount: 2,
                bodyType: "Tractor",
                driveType: ConnectedOps.Domain.Vehicles.DriveType.RWD,
                emissionStandard: "Euro 6",
                tyreSizeFront: "315/80R22.5",
                tyreSizeRear: "315/80R22.5");

            db.VehicleSpecifications.Add(spec);
            await db.SaveChangesAsync();
        }

        return (category.Id, make.Id, model.Id, vehicle.Id);
    }

    [Fact]
    public async Task FuelTypeService_Create_Update_Delete_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userA = new TestUserContext { TenantId = tenantA, UserId = Guid.NewGuid() };
        var userB = new TestUserContext { TenantId = tenantB, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();

        var serviceA = new FuelTypeService(db, userA, audit);
        var serviceB = new FuelTypeService(db, userB, audit);

        // Create custom fuel type in Tenant A
        var createReq = new CreateFuelTypeDefinitionRequest(
            Code: "B20-BIO",
            Name: "B20 Biodiesel Blend",
            FuelType: FuelType.Biodiesel,
            EnergyType: "Liquid",
            DefaultUnit: FuelUnit.Liter,
            Density: 0.85m,
            IsActive: true);

        var created = await serviceA.CreateAsync(createReq);
        Assert.NotNull(created);
        Assert.Equal("B20-BIO", created.Code);
        Assert.Equal(tenantA, created.TenantId);

        // Tenant B should not see Tenant A's custom fuel type
        var typesB = await serviceB.GetAllAsync();
        Assert.DoesNotContain(typesB, t => t.Code == "B20-BIO");

        // Tenant B cannot update Tenant A's fuel type
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            serviceB.UpdateAsync(created.Id, new UpdateFuelTypeDefinitionRequest("B20-BIO", "Hacked Bio", FuelType.Biodiesel, "Liquid", FuelUnit.Liter, 0.9m, true)));

        // Tenant A updates successfully
        var updated = await serviceA.UpdateAsync(created.Id, new UpdateFuelTypeDefinitionRequest("B20-BIO", "B20 Premium Blend", FuelType.Biodiesel, "Liquid", FuelUnit.Liter, 0.86m, true));
        Assert.Equal("B20 Premium Blend", updated.Name);

        // Duplicate code in same tenant throws ConflictException
        await Assert.ThrowsAsync<ConflictException>(() => serviceA.CreateAsync(createReq));

        // Soft delete / delete
        await serviceA.DeleteAsync(created.Id);
        var afterDelete = await serviceA.GetAllAsync();
        Assert.DoesNotContain(afterDelete, t => t.Id == created.Id);
    }

    [Fact]
    public async Task FuelStationService_Create_Update_Delete_AndQuery()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var service = new FuelStationService(db, user, audit);

        // Create internal depot station
        var req = new CreateFuelStationRequest(
            Code: "DEPOT-01",
            Name: "Main Logistics Yard Depot Fueling",
            StationType: FuelStationType.Internal,
            VendorName: "Internal Yard Fuel",
            Address: "100 Depot Way, Chicago, IL",
            Latitude: 41.8781,
            Longitude: -87.6298,
            ContactPhone: "555-0199",
            IsActive: true,
            Notes: "Primary fueling pump for overnight heavy fleet");

        var station = await service.CreateAsync(req);
        Assert.NotNull(station);
        Assert.Equal("DEPOT-01", station.Code);
        Assert.Equal(FuelStationType.Internal, station.StationType);

        // Update station
        var updateReq = new UpdateFuelStationRequest(
            Code: "DEPOT-01-EXP",
            Name: "Main Logistics Yard Depot (Expanded)",
            StationType: FuelStationType.Internal,
            VendorName: "Internal Yard Fuel",
            Address: "100 Depot Way, Chicago, IL",
            Latitude: 41.8781,
            Longitude: -87.6298,
            BranchId: null,
            ContactPhone: "555-0199",
            IsActive: true,
            Notes: "Upgraded fast-flow pumps");

        var updated = await service.UpdateAsync(station.Id, updateReq);
        Assert.Equal("DEPOT-01-EXP", updated.Code);
        Assert.Equal("Main Logistics Yard Depot (Expanded)", updated.Name);

        // Query paged
        var paged = await service.GetStationsPagedAsync(new FuelStationQueryParameters { SearchTerm = "Logistics" });
        Assert.Equal(1, paged.TotalCount);
        Assert.Single(paged.Items);
    }

    [Fact]
    public async Task FuelCardService_StrictMasking_AndStatusTransitions()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var service = new FuelCardService(db, user, audit);

        // Create card with raw 16-digit card number
        var rawCardNumber = "7008123456789012";
        var createReq = new CreateFuelCardRequest(
            CardNumber: rawCardNumber,
            CardReference: "FC-WEST-01",
            ProviderName: "WEX Fleet",
            SpendingLimit: 2500.00m,
            ExpiresAtUtc: DateTime.UtcNow.AddYears(2),
            Notes: "Assigned to Western Corridor drivers");

        var card = await service.CreateAsync(createReq);
        Assert.NotNull(card);
        // Ensure card number is strictly masked
        Assert.Equal("****-****-****-9012", card.CardNumberMasked);
        Assert.DoesNotContain("700812345678", card.CardNumberMasked);

        // Verify direct database entity stored card number is also masked
        var entity = await db.FuelCards.FindAsync(card.Id);
        Assert.NotNull(entity);
        Assert.Equal("****-****-****-9012", entity.CardNumberMasked);

        // Suspend card
        var suspended = await service.SetStatusAsync(card.Id, FuelCardStatus.Suspended);
        Assert.Equal(FuelCardStatus.Suspended, suspended.Status);

        // Reactivate card
        var activated = await service.SetStatusAsync(card.Id, FuelCardStatus.Active);
        Assert.Equal(FuelCardStatus.Active, activated.Status);
    }

    [Fact]
    public async Task FuelEfficiencyEngine_ConsecutiveFullTanks_AndIntermediatePartialFills()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, initialOdometer: 10000m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var txService = new FuelTransactionService(db, user, audit, odoService, efficiencyService, anomalyService);

        var baseTime = DateTime.UtcNow.AddDays(-10);

        // Transaction 1: Baseline Full Tank @ 10,000 km (100L)
        var tx1 = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: baseTime,
            Quantity: 100m,
            UnitPrice: 1.50m,
            IsFullTank: true,
            IsPartialFill: false,
            OdometerReading: 10000m,
            OdometerUnit: OdometerUnit.Kilometers));

        Assert.Equal(150.00m, tx1.TotalCost);
        // First full tank has no previous baseline, so HasSufficientData is false
        Assert.NotNull(tx1.Efficiency);
        Assert.False(tx1.Efficiency.HasSufficientData);

        // Transaction 2: Partial Fill @ 10,400 km (30L)
        var tx2 = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: baseTime.AddDays(2),
            Quantity: 30m,
            UnitPrice: 1.55m,
            IsFullTank: false,
            IsPartialFill: true,
            OdometerReading: 10400m,
            OdometerUnit: OdometerUnit.Kilometers));

        // Partial fill should not produce full tank efficiency directly
        Assert.Null(tx2.Efficiency);

        // Transaction 3: Full Tank @ 10,800 km (50L)
        // Distance traveled since Tx1 = 10,800 - 10,000 = 800 km.
        // Total fuel consumed over interval = 30L (Tx2 partial) + 50L (Tx3 full) = 80L.
        // Calculated efficiency = 800 km / 80 L = 10.00 km/L.
        // Liters per 100km = (80 / 800) * 100 = 10.00 L/100km.
        var tx3 = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: baseTime.AddDays(4),
            Quantity: 50m,
            UnitPrice: 1.50m,
            IsFullTank: true,
            IsPartialFill: false,
            OdometerReading: 10800m,
            OdometerUnit: OdometerUnit.Kilometers));

        Assert.NotNull(tx3.Efficiency);
        Assert.Equal(800m, tx3.Efficiency.DistanceKilometers);
        Assert.Equal(80m, tx3.Efficiency.VolumeLiters);
        Assert.Equal(10.00m, Math.Round(tx3.Efficiency.KilometersPerLiter!.Value, 2));
        Assert.Equal(10.00m, Math.Round(tx3.Efficiency.LitersPer100Km!.Value, 2));
        Assert.True(tx3.Efficiency.MilesPerGallonUS.HasValue);
        Assert.True(tx3.Efficiency.MilesPerGallonUK.HasValue);
        Assert.Equal(23.52m, Math.Round(tx3.Efficiency.MilesPerGallonUS.Value, 2)); // 10 km/L * 2.35215
    }

    [Fact]
    public void FuelUnitConverter_AccurateCalculations()
    {
        // 100 Liters -> Gallons US
        var galUs = FuelUnitConverter.FromLiters(100m, FuelUnit.GallonUS);
        Assert.Equal(26.4172m, Math.Round(galUs, 4));

        // 100 Liters -> Gallons UK
        var galUk = FuelUnitConverter.FromLiters(100m, FuelUnit.GallonImperial);
        Assert.Equal(21.9969m, Math.Round(galUk, 4));

        // 100 km -> Miles
        var miles = FuelUnitConverter.FromKilometers(100m, OdometerUnit.Miles);
        Assert.Equal(62.137m, Math.Round(miles, 3));

        // 100 km, 10 Liters -> Km/L and L/100km
        var kmPerLiter = FuelUnitConverter.CalculateKilometersPerLiter(100m, 10m);
        Assert.Equal(10.0m, kmPerLiter);

        var lPer100 = FuelUnitConverter.CalculateLitersPer100Km(100m, 10m);
        Assert.Equal(10.0m, lPer100);

        // 100 km, 10 Liters -> MPG US
        var mpgUs = FuelUnitConverter.CalculateMilesPerGallonUs(100m, 10m);
        Assert.Equal(23.521m, Math.Round(mpgUs!.Value, 3));

        // 100 km, 10 Liters -> MPG UK
        var mpgUk = FuelUnitConverter.CalculateMilesPerGallonUk(100m, 10m);
        Assert.Equal(28.248m, Math.Round(mpgUk!.Value, 3));
    }

    [Fact]
    public async Task FuelTransaction_OdometerRegression_Rejection()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, initialOdometer: 50000m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var txService = new FuelTransactionService(db, user, audit, odoService, efficiencyService, anomalyService);

        // Advance vehicle odometer to 52,000 via a fuel transaction
        await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: DateTime.UtcNow.AddDays(-1),
            Quantity: 150m,
            UnitPrice: 1.45m,
            OdometerReading: 52000m,
            OdometerUnit: OdometerUnit.Kilometers));

        var vehicle = await db.Vehicles.FindAsync(vehicleId);
        Assert.Equal(52000m, vehicle!.CurrentOdometer);

        // Attempting to log a new transaction with an older/decreased odometer (e.g. 51,000 km) should be rejected
        await Assert.ThrowsAsync<ValidationException>(() =>
            txService.CreateAsync(new CreateFuelTransactionRequest(
                VehicleId: vehicleId,
                TransactionDateUtc: DateTime.UtcNow,
                Quantity: 100m,
                UnitPrice: 1.45m,
                OdometerReading: 51000m,
                OdometerUnit: OdometerUnit.Kilometers)));
    }

    [Fact]
    public async Task FuelAnomalyDetection_TankCapacityExceeded_TriggersAnomaly()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        // Seed vehicle with tank capacity = 200 Liters
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, initialOdometer: 10000m, tankCapacity: 200m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var txService = new FuelTransactionService(db, user, audit, odoService, efficiencyService, anomalyService);

        // Log transaction with 280 Liters (140% of tank capacity > 125% threshold)
        var tx = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: DateTime.UtcNow,
            Quantity: 280m,
            UnitPrice: 1.45m,
            OdometerReading: 10500m));

        Assert.NotEmpty(tx.Anomalies);
        var tankAnomaly = tx.Anomalies.FirstOrDefault(a => a.AnomalyType == FuelAnomalyType.TankCapacityExceeded);
        Assert.NotNull(tankAnomaly);
        Assert.Equal(FuelAnomalySeverity.High, tankAnomaly.Severity);
        Assert.Equal(FuelAnomalyStatus.Open, tankAnomaly.Status);

        // Resolve anomaly
        var resolved = await anomalyService.ResolveAsync(tankAnomaly.Id, new ResolveFuelAnomalyRequest("Dual auxiliary tanks confirmed installed"));
        Assert.Equal(FuelAnomalyStatus.Resolved, resolved.Status);
        Assert.Equal("Dual auxiliary tanks confirmed installed", resolved.ResolutionNotes);
    }

    [Fact]
    public async Task FuelAnomalyDetection_HighUnitPrice_AndDuplicateFueling()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, initialOdometer: 20000m, tankCapacity: 500m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var txService = new FuelTransactionService(db, user, audit, odoService, efficiencyService, anomalyService);

        // Seed baseline historical transactions at $1.50/L with strictly increasing odometer
        for (int i = 5; i >= 1; i--)
        {
            await txService.CreateAsync(new CreateFuelTransactionRequest(
                VehicleId: vehicleId,
                TransactionDateUtc: DateTime.UtcNow.AddDays(-i),
                Quantity: 80m,
                UnitPrice: 1.50m,
                OdometerReading: 20000m + ((5 - i + 1) * 200)));
        }

        // Tx with Unit Price = $4.50/L (> 2.0x 30d avg of $1.50)
        var now = DateTime.UtcNow;
        var highPriceTx = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: now,
            Quantity: 90m,
            UnitPrice: 4.50m,
            OdometerReading: 21500m));

        Assert.Contains(highPriceTx.Anomalies, a => a.AnomalyType == FuelAnomalyType.HighUnitPrice);

        // Immediate duplicate fueling within 5 minutes with same vehicle & quantity
        var dupTx = await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: now.AddMinutes(5),
            Quantity: 90m,
            UnitPrice: 1.50m,
            OdometerReading: 21500m));

        Assert.Contains(dupTx.Anomalies, a => a.AnomalyType == FuelAnomalyType.DuplicateFueling);
    }

    [Fact]
    public async Task FuelImportService_ProcessBatch_SuccessAndErrorHandling()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, vehicleNumber: "IMP-VEH-01", initialOdometer: 30000m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var importService = new FuelImportService(db, user, audit, odoService, anomalyService);

        var rows = new List<ImportFuelTransactionRowRequest>
        {
            // Valid row 1
            new(
                RowNumber: 1,
                VehicleNumberOrRegistration: "IMP-VEH-01",
                TransactionDateUtc: DateTime.UtcNow.AddDays(-2),
                Quantity: 120m,
                UnitPrice: 1.48m,
                TotalCost: 177.60m,
                OdometerReading: 30500m,
                IsFullTank: true,
                ExternalTransactionId: "EXT-IMP-001"),

            // Invalid row 2 (non-existent vehicle)
            new(
                RowNumber: 2,
                VehicleNumberOrRegistration: "GHOST-VEH-99",
                TransactionDateUtc: DateTime.UtcNow.AddDays(-1),
                Quantity: 80m,
                UnitPrice: 1.50m,
                TotalCost: 120.00m,
                OdometerReading: 40000m,
                IsFullTank: true,
                ExternalTransactionId: "EXT-IMP-002")
        };

        var batch = await importService.ImportTransactionsAsync(new ImportFuelTransactionsRequest(
            FileName: "vendor_fuel_report_sep.csv",
            Rows: rows));

        Assert.NotNull(batch);
        Assert.Equal(2, batch.TotalRows);
        Assert.Equal(1, batch.ImportedRows);
        Assert.Equal(1, batch.RejectedRows);
        Assert.Equal(FuelImportStatus.CompletedWithErrors, batch.Status);
        Assert.Single(batch.Errors);
        Assert.Equal(2, batch.Errors.First().RowNumber);
    }

    [Fact]
    public async Task FuelDashboard_AndAnalytics_ComputesCorrectMetrics()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var user = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var audit = new TestAuditLogService();
        var (_, _, _, vehicleId) = await SeedVehicleAsync(db, tenantId, vehicleNumber: "STAT-01", initialOdometer: 10000m);

        var odoService = new VehicleOdometerService(db, user, audit);
        var efficiencyService = new FuelEfficiencyService(db);
        var anomalyService = new FuelAnomalyService(db, user, audit, efficiencyService);
        var txService = new FuelTransactionService(db, user, audit, odoService, efficiencyService, anomalyService);
        var analyticsService = new FuelAnalyticsService(db, user, efficiencyService);
        var dashboardService = new FuelDashboardService(db, user, analyticsService, txService, anomalyService);

        // Record two transactions in current month
        await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: DateTime.UtcNow.AddDays(-3),
            Quantity: 100m,
            UnitPrice: 1.50m,
            OdometerReading: 10500m,
            IsFullTank: true));

        await txService.CreateAsync(new CreateFuelTransactionRequest(
            VehicleId: vehicleId,
            TransactionDateUtc: DateTime.UtcNow.AddDays(-1),
            Quantity: 100m,
            UnitPrice: 1.60m,
            OdometerReading: 11500m,
            IsFullTank: true));

        // Get Vehicle Summary
        var vehicleSummary = await analyticsService.GetVehicleFuelSummaryAsync(vehicleId);
        Assert.NotNull(vehicleSummary);
        Assert.Equal(2, vehicleSummary.TotalTransactions);
        Assert.Equal(200m, vehicleSummary.FuelQuantityThisMonthLiters);
        Assert.Equal(310m, vehicleSummary.FuelCostThisMonth);
        Assert.Equal(10.0m, vehicleSummary.AverageEfficiencyKmPerLiter);

        // Get Dashboard
        var dashboard = await dashboardService.GetDashboardMetricsAsync();
        Assert.NotNull(dashboard);
        Assert.Equal(200m, dashboard.FuelQuantityThisMonthLiters);
        Assert.Equal(310m, dashboard.FuelCostThisMonth);
        Assert.Equal(2, dashboard.RecentTransactions.Count);
    }
}
