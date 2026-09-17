using ConnectedOps.Application.Ev;
using ConnectedOps.Application.Optimization;
using ConnectedOps.Application.WhiteLabel;
using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Optimization;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Domain.WhiteLabel;
using ConnectedOps.Infrastructure.Ev;
using ConnectedOps.Infrastructure.Optimization;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.WhiteLabel;
using ConnectedOps.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class Phases18To20Tests
{
    private static async Task<Vehicle> SeedVehicleAsync(
        ConnectedOpsDbContext db,
        Guid tenantId,
        string vehicleNumber = "EV-TRUCK-01")
    {
        var category = new VehicleCategory(tenantId, "Electric Delivery Van", "EV-VAN", null, true);
        var make = new VehicleMake(tenantId, "Tesla", "US");
        db.VehicleCategories.Add(category);
        db.VehicleMakes.Add(make);
        await db.SaveChangesAsync();

        var model = new VehicleModel(tenantId, make.Id, "Semi", category.Id);
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync();

        var vehicle = new Vehicle(
            tenantId,
            vehicleNumber,
            category.Id,
            make.Id,
            model.Id,
            displayName: "Tesla Semi Commercial",
            registrationNumber: "CA-EV-882",
            vin: "5YJSA1E21HF000101",
            fuelType: FuelType.Electric,
            status: VehicleStatus.Active);

        db.Vehicles.Add(vehicle);
        await db.SaveChangesAsync();
        return vehicle;
    }

    // =========================================================================
    // PHASE 18: EV FLEET MANAGEMENT & SMART CHARGING TESTS
    // =========================================================================

    [Fact]
    public async Task Phase18_CreateStation_RegistersSuccessfullyWithPlugs()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new EvService(db, userContext, NullLogger<EvService>.Instance);

        var station = await service.CreateStationAsync(new CreateChargingStationRequest(
            Code: "DEPOT-DC-01",
            Name: "Central Logistics Supercharger",
            StationType: ChargingStationType.DepotPrivate,
            ConnectorType: ChargingConnectorType.Ccs2,
            MaxPowerKw: 250m,
            TotalPlugs: 4,
            Address: "100 Logistics Blvd",
            OffPeakRatePerKwh: 0.10m,
            PeakRatePerKwh: 0.28m));

        Assert.NotNull(station);
        Assert.Equal("DEPOT-DC-01", station.Code);
        Assert.Equal(4, station.TotalPlugs);
        Assert.Equal(4, station.AvailablePlugs);
        Assert.True(station.IsActive);
    }

    [Fact]
    public async Task Phase18_UpdateTelemetry_UpdatesBatteryStateAndHealth()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId);
        var service = new EvService(db, userContext, NullLogger<EvService>.Instance);

        var batteryDto = await service.UpdateTelemetryAsync(new UpdateVehicleBatteryTelemetryRequest(
            VehicleId: vehicle.Id,
            StateOfChargePercent: 78.5m,
            RemainingRangeKm: 340m,
            BatteryPackTempCelsius: 28.5m,
            ChargingStatus: EvChargingStatus.ChargingAc,
            ActiveChargingPowerKw: 22m));

        Assert.NotNull(batteryDto);
        Assert.Equal(78.5m, batteryDto.StateOfChargePercent);
        Assert.Equal(340m, batteryDto.RemainingRangeKm);
        Assert.Equal(EvChargingStatus.ChargingAc, batteryDto.ChargingStatus);
        Assert.Equal(BatteryHealthCondition.Optimal, batteryDto.HealthCondition);
    }

    [Fact]
    public async Task Phase18_ChargingSession_Lifecycle_TracksEnergyAndOccupancy()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId);
        var service = new EvService(db, userContext, NullLogger<EvService>.Instance);

        var station = await service.CreateStationAsync(new CreateChargingStationRequest(
            Code: "DEPOT-02",
            Name: "Fast Charger 2",
            StationType: ChargingStationType.DepotPrivate,
            ConnectorType: ChargingConnectorType.Ccs2,
            MaxPowerKw: 150m,
            TotalPlugs: 2));

        // Start session
        var session = await service.StartSessionAsync(new StartChargingSessionRequest(
            VehicleId: vehicle.Id,
            ChargingStationId: station.Id,
            StartSocPercent: 20m));

        Assert.NotNull(session);
        Assert.True(session.IsActive);
        Assert.Equal(20m, session.StartSocPercent);

        // Verify available plugs decremented
        var updatedStation = (await service.GetStationsAsync()).First(s => s.Id == station.Id);
        Assert.Equal(1, updatedStation.AvailablePlugs);

        // Complete session
        var completed = await service.CompleteSessionAsync(new CompleteChargingSessionRequest(
            SessionId: session.Id,
            EndSocPercent: 90m,
            EnergyDeliveredKwh: 60m,
            TotalCost: 12.50m));

        Assert.False(completed.IsActive);
        Assert.Equal(90m, completed.EndSocPercent);
        Assert.Equal(60m, completed.EnergyDeliveredKwh);
        Assert.Equal(12.50m, completed.TotalCost);
        Assert.True(completed.Co2SavedKg > 0);

        // Verify plug freed up
        var freedStation = (await service.GetStationsAsync()).First(s => s.Id == station.Id);
        Assert.Equal(2, freedStation.AvailablePlugs);
    }

    [Fact]
    public async Task Phase18_ConfigureSmartCharging_EnforcesPolicy()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle = await SeedVehicleAsync(db, tenantId);
        var service = new EvService(db, userContext, NullLogger<EvService>.Instance);

        // Init battery
        await service.UpdateTelemetryAsync(new UpdateVehicleBatteryTelemetryRequest(
            VehicleId: vehicle.Id,
            StateOfChargePercent: 50m,
            RemainingRangeKm: 200m,
            BatteryPackTempCelsius: 25m,
            ChargingStatus: EvChargingStatus.Disconnected,
            ActiveChargingPowerKw: 0m));

        var updated = await service.ConfigureSmartChargingAsync(new ConfigureSmartChargingRequest(
            VehicleId: vehicle.Id,
            TargetSocLimitPercent: 80,
            IsOffPeakOnlyCharging: true));

        Assert.Equal(80, updated.TargetSocLimitPercent);
        Assert.True(updated.IsOffPeakOnlyCharging);
    }

    [Fact]
    public async Task Phase18_EvService_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var serviceA = new EvService(db, new TestUserContext { TenantId = tenantA }, NullLogger<EvService>.Instance);
        var serviceB = new EvService(db, new TestUserContext { TenantId = tenantB }, NullLogger<EvService>.Instance);

        await serviceA.CreateStationAsync(new CreateChargingStationRequest("STATION-A", "Depot A", ChargingStationType.DepotPrivate, ChargingConnectorType.Ccs2, 100m, 2));

        var stationsA = await serviceA.GetStationsAsync();
        var stationsB = await serviceB.GetStationsAsync();

        Assert.Single(stationsA);
        Assert.Empty(stationsB);
    }

    // =========================================================================
    // PHASE 19: AI ROUTE OPTIMIZATION & VRP SOLVER TESTS
    // =========================================================================

    [Fact]
    public async Task Phase19_SolveVrp_SequencesStopsAndAllocatesVehicles()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var vehicle1 = await SeedVehicleAsync(db, tenantId, "VAN-01");
        var vehicle2 = await SeedVehicleAsync(db, tenantId, "VAN-02");

        var service = new RouteOptimizationService(db, userContext, NullLogger<RouteOptimizationService>.Instance);

        var stops = new List<StopInputRequest>
        {
            new("Stop 1", "Address 1", 37.7750, -122.4180, 50m),
            new("Stop 2", "Address 2", 37.7800, -122.4100, 30m),
            new("Stop 3", "Address 3", 37.7850, -122.4050, 40m),
            new("Stop 4", "Address 4", 37.7900, -122.4000, 60m),
            new("Stop 5", "Address 5", 37.7950, -122.3950, 25m)
        };

        var run = await service.SolveVrpAsync(new CreateOptimizationRunRequest(
            Objective: OptimizationObjective.MinimizeDistance,
            Stops: stops,
            DepotAddress: "Main Distribution Center",
            DepotLatitude: 37.7700,
            DepotLongitude: -122.4200));

        Assert.NotNull(run);
        Assert.StartsWith("VRP-", run.RunNumber);
        Assert.Equal(OptimizationRunStatus.Completed, run.Status);
        Assert.Equal(5, run.TotalStopsInput);
        Assert.True(run.VehiclesAllocated >= 1);
        Assert.True(run.TotalDistanceKm > 0m);
        Assert.True(run.TotalDurationMinutes > 0);
        Assert.True(run.EfficiencyScorePercent > 80m);
        Assert.NotEmpty(run.RoutePlans);

        // Verify each route plan contains DepotStart and DepotEnd
        foreach (var plan in run.RoutePlans)
        {
            Assert.Contains(plan.Stops, s => s.StopType == OptimizedStopType.DepotStart);
            Assert.Contains(plan.Stops, s => s.StopType == OptimizedStopType.DepotEnd);
            Assert.Contains(plan.Stops, s => s.StopType == OptimizedStopType.Delivery);
        }
    }

    [Fact]
    public async Task Phase19_DispatchRun_UpdatesRunAndPlanStatus()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        await SeedVehicleAsync(db, tenantId, "TRUCK-D1");

        var service = new RouteOptimizationService(db, userContext, NullLogger<RouteOptimizationService>.Instance);

        var run = await service.SolveVrpAsync(new CreateOptimizationRunRequest(
            Objective: OptimizationObjective.MinimizeDuration,
            Stops: [new("Point Alpha", "100 1st St", 37.78, -122.40, 50m)]));

        var dispatched = await service.DispatchRunAsync(new DispatchOptimizationRunRequest(run.Id));

        Assert.Equal(OptimizationRunStatus.Dispatched, dispatched.Status);
        Assert.NotNull(dispatched.DispatchedAtUtc);
        Assert.All(dispatched.RoutePlans, p => Assert.NotNull(p.DispatchedJobId));
    }

    [Fact]
    public async Task Phase19_RouteOptimization_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await SeedVehicleAsync(db, tenantA, "VEH-A");
        await SeedVehicleAsync(db, tenantB, "VEH-B");

        var serviceA = new RouteOptimizationService(db, new TestUserContext { TenantId = tenantA }, NullLogger<RouteOptimizationService>.Instance);
        var serviceB = new RouteOptimizationService(db, new TestUserContext { TenantId = tenantB }, NullLogger<RouteOptimizationService>.Instance);

        var runA = await serviceA.SolveVrpAsync(new CreateOptimizationRunRequest(
            Objective: OptimizationObjective.MinimizeDistance,
            Stops: [new("Stop A", "Addr A", 37.7, -122.4, 20m)]));

        var pagedA = await serviceA.GetOptimizationRunsPagedAsync(new OptimizationFilterRequest());
        var pagedB = await serviceB.GetOptimizationRunsPagedAsync(new OptimizationFilterRequest());

        Assert.Single(pagedA.Items);
        Assert.Empty(pagedB.Items);
    }

    // =========================================================================
    // PHASE 20: ENTERPRISE WHITE-LABELING & AUDIT COMPLIANCE TESTS
    // =========================================================================

    [Fact]
    public async Task Phase20_Branding_UpdateAndRetrieve_PersistsStyles()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new WhiteLabelService(db, userContext, NullLogger<WhiteLabelService>.Instance);

        var updated = await service.UpdateBrandingAsync(new UpdateBrandingRequest(
            PlatformTitle: "Apex Global Logistics",
            LogoUrl: "https://apex.com/logo.png",
            FaviconUrl: "https://apex.com/favicon.ico",
            PrimaryAccentColor: "#1e3a8a",
            SecondaryAccentColor: "#059669",
            SupportEmail: "fleet-ops@apex.com",
            CustomLoginBannerUrl: "https://apex.com/banner.jpg",
            CustomFooterText: "Apex Enterprise Confidential",
            IsCustomBrandingEnabled: true));

        Assert.Equal("Apex Global Logistics", updated.PlatformTitle);
        Assert.Equal("#1e3a8a", updated.PrimaryAccentColor);
        Assert.Equal("#059669", updated.SecondaryAccentColor);
        Assert.True(updated.IsCustomBrandingEnabled);

        var retrieved = await service.GetBrandingAsync();
        Assert.Equal("Apex Global Logistics", retrieved.PlatformTitle);
    }

    [Fact]
    public async Task Phase20_CustomDomains_RegisterAndVerify_ManagesLifecycle()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new WhiteLabelService(db, userContext, NullLogger<WhiteLabelService>.Instance);

        var domain = await service.RegisterCustomDomainAsync(new RegisterCustomDomainRequest("fleet.apexlogistics.com"));

        Assert.NotNull(domain);
        Assert.Equal("fleet.apexlogistics.com", domain.Hostname);
        Assert.Equal(DomainVerificationStatus.PendingDns, domain.Status);
        Assert.StartsWith("co-verify-", domain.VerificationToken);
        Assert.False(domain.SslProvisioned);

        // Verify domain
        var verified = await service.VerifyCustomDomainAsync(new VerifyCustomDomainRequest(domain.Id));

        Assert.Equal(DomainVerificationStatus.Verified, verified.Status);
        Assert.True(verified.SslProvisioned);
        Assert.NotNull(verified.VerifiedAtUtc);
    }

    [Fact]
    public async Task Phase20_GenerateAuditPackage_CreatesCryptographicEvidenceBundle()
    {
        using var db = TestDbContextFactory.Create();
        var tenantId = Guid.NewGuid();
        var userContext = new TestUserContext { TenantId = tenantId, UserId = Guid.NewGuid() };
        var service = new WhiteLabelService(db, userContext, NullLogger<WhiteLabelService>.Instance);

        var package = await service.GenerateAuditPackageAsync(new GenerateAuditPackageRequest(AuditPackageType.Soc2Type2));

        Assert.NotNull(package);
        Assert.StartsWith("AUDIT-", package.PackageNumber);
        Assert.Equal(AuditPackageType.Soc2Type2, package.PackageType);
        Assert.False(string.IsNullOrWhiteSpace(package.ChecksumSha256));
        Assert.Equal(64, package.ChecksumSha256.Length); // Valid SHA-256 hex string
        Assert.True(package.FileSizeBytes > 0);
        Assert.Contains("CC6.1", package.ManifestJson);
    }

    [Fact]
    public async Task Phase20_WhiteLabel_EnforcesTenantIsolation()
    {
        using var db = TestDbContextFactory.Create();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var serviceA = new WhiteLabelService(db, new TestUserContext { TenantId = tenantA }, NullLogger<WhiteLabelService>.Instance);
        var serviceB = new WhiteLabelService(db, new TestUserContext { TenantId = tenantB }, NullLogger<WhiteLabelService>.Instance);

        await serviceA.RegisterCustomDomainAsync(new RegisterCustomDomainRequest("fleet.brand-a.com"));
        await serviceB.RegisterCustomDomainAsync(new RegisterCustomDomainRequest("fleet.brand-b.com"));

        var overviewA = await serviceA.GetOverviewAsync();
        var overviewB = await serviceB.GetOverviewAsync();

        Assert.Single(overviewA.CustomDomains);
        Assert.Equal("fleet.brand-a.com", overviewA.CustomDomains[0].Hostname);

        Assert.Single(overviewB.CustomDomains);
        Assert.Equal("fleet.brand-b.com", overviewB.CustomDomains[0].Hostname);
    }
}
