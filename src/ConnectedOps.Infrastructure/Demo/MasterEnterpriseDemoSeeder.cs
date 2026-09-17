using System.Security.Cryptography;
using System.Text.Json;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Demo;
using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Ev;
using ConnectedOps.Domain.Optimization;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Predictive;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Domain.WhiteLabel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Demo;

public sealed class MasterEnterpriseDemoSeeder : IMasterEnterpriseDemoSeeder
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<MasterEnterpriseDemoSeeder> _logger;

    public MasterEnterpriseDemoSeeder(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<MasterEnterpriseDemoSeeder> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    public async Task<EnterpriseDemoSeedingResultDto> SeedEnterpriseDemoDataAsync(
        Guid? targetTenantId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Resolve Tenant
        var tenantId = targetTenantId
            ?? _currentUserContext.TenantId
            ?? (await _dbContext.Tenants.OrderBy(t => t.CreatedAtUtc).Select(t => t.Id).FirstOrDefaultAsync(cancellationToken));

        if (tenantId == Guid.Empty)
        {
            var newTenant = new Tenant("Apex Global Logistics", "APEX-GLOBAL");
            _dbContext.Tenants.Add(newTenant);
            await _dbContext.SaveChangesAsync(cancellationToken);
            tenantId = newTenant.Id;
        }

        var tenant = await _dbContext.Tenants.FirstAsync(t => t.Id == tenantId, cancellationToken);
        _logger.LogInformation("Beginning Master Enterprise Demo Seeding for Tenant {TenantName} ({TenantId})", tenant.Name, tenantId);

        // 2. Organization Structure
        var branchHq = await _dbContext.Branches.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Code == "BR-SF", cancellationToken);
        if (branchHq == null)
        {
            branchHq = new Branch(tenantId, "San Francisco Global HQ", "BR-SF", BranchType.HeadOffice, true);
            var branchChicago = new Branch(tenantId, "Chicago Mid-West Terminal", "BR-CHI", BranchType.RegionalOffice, false);
            var branchDallas = new Branch(tenantId, "Dallas South Depot", "BR-DFW", BranchType.Depot, false);
            _dbContext.Branches.AddRange(branchHq, branchChicago, branchDallas);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 3. Vehicle Categories & Makes/Models
        var evCategory = await _dbContext.VehicleCategories.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == "CAT-EV-COMM", cancellationToken);
        if (evCategory == null)
        {
            evCategory = new VehicleCategory(tenantId, "Commercial Electric Vehicle", "CAT-EV-COMM", "Zero-emission commercial delivery vans and Class 8 electric haulers", true);
            var dslCategory = new VehicleCategory(tenantId, "Heavy Duty Diesel Tractor", "CAT-DSL-HVY", "Class 8 interstate freight diesel tractors", true);
            _dbContext.VehicleCategories.AddRange(evCategory, dslCategory);

            var teslaMake = new VehicleMake(tenantId, "Tesla Commercial", "US");
            var fordMake = new VehicleMake(tenantId, "Ford Pro", "US");
            var rivianMake = new VehicleMake(tenantId, "Rivian Commercial", "US");
            var volvoMake = new VehicleMake(tenantId, "Volvo Trucks", "SE");
            _dbContext.VehicleMakes.AddRange(teslaMake, fordMake, rivianMake, volvoMake);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var semiModel = new VehicleModel(tenantId, teslaMake.Id, "Semi Class 8 (500kWh)", evCategory.Id);
            var eTransitModel = new VehicleModel(tenantId, fordMake.Id, "E-Transit 350 Cargo", evCategory.Id);
            var edvModel = new VehicleModel(tenantId, rivianMake.Id, "EDV 700 Delivery", evCategory.Id);
            var fh16Model = new VehicleModel(tenantId, volvoMake.Id, "FH16 750 Aero", dslCategory.Id);
            _dbContext.VehicleModels.AddRange(semiModel, eTransitModel, edvModel, fh16Model);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var semiModelRef = await _dbContext.VehicleModels.FirstAsync(m => m.TenantId == tenantId && m.Name.Contains("Semi"), cancellationToken);
        var eTransitModelRef = await _dbContext.VehicleModels.FirstAsync(m => m.TenantId == tenantId && m.Name.Contains("E-Transit"), cancellationToken);
        var edvModelRef = await _dbContext.VehicleModels.FirstAsync(m => m.TenantId == tenantId && m.Name.Contains("EDV"), cancellationToken);
        var fh16ModelRef = await _dbContext.VehicleModels.FirstAsync(m => m.TenantId == tenantId && m.Name.Contains("FH16"), cancellationToken);

        // 4. Vehicles
        var v1 = await _dbContext.Vehicles.FirstOrDefaultAsync(v => v.TenantId == tenantId && v.VehicleNumber == "EV-SEMI-01", cancellationToken);
        if (v1 == null)
        {
            v1 = new Vehicle(tenantId, "EV-SEMI-01", evCategory.Id, semiModelRef.VehicleMakeId, semiModelRef.Id,
                displayName: "Tesla Semi Class 8 (Long Haul)",
                registrationNumber: "CA-EV-901",
                vin: "5YJSA1E21HF000101",
                fuelType: FuelType.Electric,
                currentOdometer: 42150m,
                branchId: branchHq.Id);

            var v2 = new Vehicle(tenantId, "EV-VAN-02", evCategory.Id, eTransitModelRef.VehicleMakeId, eTransitModelRef.Id,
                displayName: "Ford E-Transit 350 Express",
                registrationNumber: "CA-EV-902",
                vin: "1FTNE3Y89PK100202",
                fuelType: FuelType.Electric,
                currentOdometer: 18400m,
                branchId: branchHq.Id);

            var v3 = new Vehicle(tenantId, "EV-RIV-03", evCategory.Id, edvModelRef.VehicleMakeId, edvModelRef.Id,
                displayName: "Rivian Commercial EDV 700",
                registrationNumber: "CA-EV-903",
                vin: "7FCE85519PN200303",
                fuelType: FuelType.Electric,
                currentOdometer: 11200m,
                branchId: branchHq.Id);

            var dslCategoryRef = await _dbContext.VehicleCategories.FirstAsync(c => c.TenantId == tenantId && c.Code == "CAT-DSL-HVY", cancellationToken);
            var v4 = new Vehicle(tenantId, "DSL-TRK-04", dslCategoryRef.Id, fh16ModelRef.VehicleMakeId, fh16ModelRef.Id,
                displayName: "Volvo FH16 Interstate Heavy",
                registrationNumber: "TX-HVY-404",
                vin: "YV2RT40A9PB400404",
                fuelType: FuelType.Diesel,
                currentOdometer: 185200m,
                branchId: branchHq.Id);

            _dbContext.Vehicles.AddRange(v1, v2, v3, v4);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var evVehicles = await _dbContext.Vehicles.Where(v => v.TenantId == tenantId && v.FuelType == FuelType.Electric).ToListAsync(cancellationToken);
        var semiVehicle = evVehicles.First(v => v.VehicleNumber == "EV-SEMI-01");
        var vanVehicle = evVehicles.First(v => v.VehicleNumber == "EV-VAN-02");
        var rivianVehicle = evVehicles.First(v => v.VehicleNumber == "EV-RIV-03");
        var volvoVehicle = await _dbContext.Vehicles.FirstAsync(v => v.TenantId == tenantId && v.VehicleNumber == "DSL-TRK-04", cancellationToken);

        // 5. Drivers
        var d1 = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.TenantId == tenantId && d.DriverNumber == "DRV-101", cancellationToken);
        if (d1 == null)
        {
            d1 = new Driver(tenantId, "DRV-101", "Marcus", "Vance", "+1-415-555-0101", DriverType.Employee, email: "marcus.vance@apexlogistics.com");
            var d2 = new Driver(tenantId, "DRV-102", "Elena", "Rostova", "+1-415-555-0102", DriverType.Employee, email: "elena.rostova@apexlogistics.com");
            var d3 = new Driver(tenantId, "DRV-103", "Derrick", "Hayes", "+1-415-555-0103", DriverType.Employee, email: "derrick.hayes@apexlogistics.com");
            _dbContext.Drivers.AddRange(d1, d2, d3);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        var driver1 = await _dbContext.Drivers.FirstAsync(d => d.TenantId == tenantId && d.DriverNumber == "DRV-101", cancellationToken);
        var driver2 = await _dbContext.Drivers.FirstAsync(d => d.TenantId == tenantId && d.DriverNumber == "DRV-102", cancellationToken);

        // 6. Phase 18: EV Charging Stations & Battery Telemetry
        var st1 = await _dbContext.ChargingStations.FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Code == "DEPOT-DC-01", cancellationToken);
        if (st1 == null)
        {
            st1 = new ChargingStation(tenantId, "DEPOT-DC-01", "San Francisco Megawatt Fast Depot", ChargingStationType.DepotPrivate, ChargingConnectorType.Ccs2, 350m, 4,
                "500 7th St, San Francisco, CA", 37.7735, -122.4042, 0.11m, 0.32m);

            var st2 = new ChargingStation(tenantId, "DEPOT-NACS-02", "Tesla NACS Fleet Supercharger", ChargingStationType.DepotPrivate, ChargingConnectorType.TeslaNacs, 250m, 6,
                "500 7th St, San Francisco, CA", 37.7735, -122.4042, 0.12m, 0.34m);

            var st3 = new ChargingStation(tenantId, "DEPOT-AC-03", "Overnight Depot AC Fleet Bank", ChargingStationType.DepotPrivate, ChargingConnectorType.Type2Menno, 22m, 8,
                "500 7th St, San Francisco, CA", 37.7735, -122.4042, 0.09m, 0.25m);

            _dbContext.ChargingStations.AddRange(st1, st2, st3);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // EV Batteries
        var bat1 = await _dbContext.VehicleBatteryStates.FirstOrDefaultAsync(b => b.TenantId == tenantId && b.VehicleId == semiVehicle.Id, cancellationToken);
        if (bat1 == null)
        {
            bat1 = new VehicleBatteryState(tenantId, semiVehicle.Id, 500m, 84.5m, 98.2m, 620m, 27m, 48, 85, true);
            var bat2 = new VehicleBatteryState(tenantId, vanVehicle.Id, 68m, 42.0m, 95.0m, 115m, 29m, 120, 80, true);
            bat2.UpdateTelemetry(42.0m, 115m, 29m, EvChargingStatus.ChargingAc, 11m);

            var bat3 = new VehicleBatteryState(tenantId, rivianVehicle.Id, 100m, 22.5m, 99.0m, 78m, 32m, 15, 90, false);
            bat3.UpdateTelemetry(22.5m, 78m, 32m, EvChargingStatus.ChargingDcFast, 150m);

            _dbContext.VehicleBatteryStates.AddRange(bat1, bat2, bat3);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Charging Sessions
        var activeSessionCount = await _dbContext.ChargingSessions.CountAsync(s => s.TenantId == tenantId, cancellationToken);
        if (activeSessionCount == 0)
        {
            // Active session 1: Ford E-Transit at AC station
            var sess1 = new ChargingSession(tenantId, vanVehicle.Id, st1.Id, 25m, false);
            // Completed session: Tesla Semi
            var sess2 = new ChargingSession(tenantId, semiVehicle.Id, st1.Id, 15m, false);
            sess2.CompleteSession(85m, 350m, 38.50m);

            _dbContext.ChargingSessions.AddRange(sess1, sess2);
            st1.UpdateOccupancy(1);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 7. Phase 19: AI Route Optimization & Multi-Stop VRP
        var vrpRun = await _dbContext.RouteOptimizationRuns.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.RunNumber == "VRP-SF-BAY-DEMO", cancellationToken);
        if (vrpRun == null)
        {
            vrpRun = new RouteOptimizationRun(tenantId, "VRP-SF-BAY-DEMO", OptimizationObjective.MinimizeDistance, 6, 2);
            _dbContext.RouteOptimizationRuns.Add(vrpRun);

            // Plan 1: Ford E-Transit
            var plan1 = new OptimizedRoutePlan(tenantId, vrpRun.Id, vanVehicle.Id, driver1.Id,
                $"Route 1: {vanVehicle.DisplayName}", 34.8m, 115, 140m);
            typeof(OptimizedRoutePlan).GetProperty("DistanceKm")?.SetValue(plan1, 34.8m);
            typeof(OptimizedRoutePlan).GetProperty("DurationMinutes")?.SetValue(plan1, 115);
            typeof(OptimizedRoutePlan).GetProperty("PayloadWeightKg")?.SetValue(plan1, 140m);

            plan1.AddStop(new OptimizedStopSequence(tenantId, plan1.Id, 1, OptimizedStopType.DepotStart, "Depot Departure", "500 7th St, San Francisco, CA", 37.7735, -122.4042, DateTime.UtcNow.Date.AddHours(8), DateTime.UtcNow.Date.AddHours(8).AddMinutes(15)));
            plan1.AddStop(new OptimizedStopSequence(tenantId, plan1.Id, 2, OptimizedStopType.Delivery, "Downtown Financial Center", "100 Market St, San Francisco, CA", 37.7937, -122.3965, DateTime.UtcNow.Date.AddHours(8).AddMinutes(35), DateTime.UtcNow.Date.AddHours(8).AddMinutes(45), "Alice Walker (555-0101)", 50m));
            plan1.AddStop(new OptimizedStopSequence(tenantId, plan1.Id, 3, OptimizedStopType.Delivery, "Marina Bay Gourmet", "2100 Chestnut St, San Francisco, CA", 37.8005, -122.4370, DateTime.UtcNow.Date.AddHours(9).AddMinutes(10), DateTime.UtcNow.Date.AddHours(9).AddMinutes(25), "David Kim (555-0104)", 90m));
            plan1.AddStop(new OptimizedStopSequence(tenantId, plan1.Id, 4, OptimizedStopType.DepotEnd, "Depot Return", "500 7th St, San Francisco, CA", 37.7735, -122.4042, DateTime.UtcNow.Date.AddHours(9).AddMinutes(55), DateTime.UtcNow.Date.AddHours(10)));

            // Plan 2: Rivian EDV
            var plan2 = new OptimizedRoutePlan(tenantId, vrpRun.Id, rivianVehicle.Id, driver2.Id,
                $"Route 2: {rivianVehicle.DisplayName}", 28.5m, 95, 190m);
            typeof(OptimizedRoutePlan).GetProperty("DistanceKm")?.SetValue(plan2, 28.5m);
            typeof(OptimizedRoutePlan).GetProperty("DurationMinutes")?.SetValue(plan2, 95);
            typeof(OptimizedRoutePlan).GetProperty("PayloadWeightKg")?.SetValue(plan2, 190m);

            plan2.AddStop(new OptimizedStopSequence(tenantId, plan2.Id, 1, OptimizedStopType.DepotStart, "Depot Departure", "500 7th St, San Francisco, CA", 37.7735, -122.4042, DateTime.UtcNow.Date.AddHours(8), DateTime.UtcNow.Date.AddHours(8).AddMinutes(15)));
            plan2.AddStop(new OptimizedStopSequence(tenantId, plan2.Id, 2, OptimizedStopType.Delivery, "Mission District Distribution", "2450 Mission St, San Francisco, CA", 37.7587, -122.4190, DateTime.UtcNow.Date.AddHours(8).AddMinutes(30), DateTime.UtcNow.Date.AddHours(8).AddMinutes(45), "Carlos Mendez (555-0102)", 80m));
            plan2.AddStop(new OptimizedStopSequence(tenantId, plan2.Id, 3, OptimizedStopType.Delivery, "Potrero Hill Logistics Hub", "300 16th St, San Francisco, CA", 37.7668, -122.3912, DateTime.UtcNow.Date.AddHours(9).AddMinutes(05), DateTime.UtcNow.Date.AddHours(9).AddMinutes(20), "Emma Stone (555-0105)", 110m));
            plan2.AddStop(new OptimizedStopSequence(tenantId, plan2.Id, 4, OptimizedStopType.DepotEnd, "Depot Return", "500 7th St, San Francisco, CA", 37.7735, -122.4042, DateTime.UtcNow.Date.AddHours(9).AddMinutes(35), DateTime.UtcNow.Date.AddHours(9).AddMinutes(45)));

            vrpRun.AddPlan(plan1);
            vrpRun.AddPlan(plan2);

            vrpRun.CompleteOptimization(2, 63.3m, 210, 330m, 95.2m, "{\"status\":\"Optimal\",\"algorithm\":\"CVRPTW-2Opt\"}");
            vrpRun.MarkDispatched();

            _dbContext.OptimizedRoutePlans.AddRange(plan1, plan2);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 8. Phase 20: Enterprise White-Labeling & Audit Compliance
        var branding = await _dbContext.TenantBrandings.FirstOrDefaultAsync(b => b.TenantId == tenantId, cancellationToken);
        if (branding == null)
        {
            branding = new TenantBranding(
                tenantId: tenantId,
                platformTitle: "Apex Global Logistics",
                logoUrl: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=120&h=120&fit=crop",
                faviconUrl: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=32&h=32&fit=crop",
                primaryAccentColor: "#1e3a8a",
                secondaryAccentColor: "#059669",
                supportEmail: "operations@apexlogistics.com",
                customLoginBannerUrl: "https://images.unsplash.com/photo-1586528116311-ad8dd3c8310d?w=1600&fit=crop",
                customFooterText: "Apex Enterprise Global Logistics &bull; Powered by ConnectedOps Cloud",
                isCustomBrandingEnabled: true);
            _dbContext.TenantBrandings.Add(branding);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var domain = await _dbContext.TenantCustomDomains.FirstOrDefaultAsync(d => d.TenantId == tenantId, cancellationToken);
        if (domain == null)
        {
            domain = new TenantCustomDomain(tenantId, "fleet.apexlogistics.com");
            domain.MarkVerified();
            _dbContext.TenantCustomDomains.Add(domain);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var auditPkg = await _dbContext.AuditCompliancePackages.FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
        if (auditPkg == null)
        {
            var manifestData = new
            {
                PackageNumber = "AUDIT-SOC2-APEX-01",
                Framework = "SOC 2 Type II / ISO 27001 ISMS",
                GeneratedAtUtc = DateTime.UtcNow,
                ControlsEvaluated = new[] { "CC6.1 Logical Access", "CC6.6 Boundary Protection", "CC7.2 Log Auditing" }
            };
            var json = JsonSerializer.Serialize(manifestData);
            var sha = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json))).ToLowerInvariant();

            auditPkg = new AuditCompliancePackage(
                tenantId: tenantId,
                packageNumber: "AUDIT-SOC2-APEX-01",
                packageType: AuditPackageType.Soc2Type2,
                generatedByUserId: Guid.Parse("00000000-0000-0000-0000-000000000001"),
                evidenceItemsCount: 64,
                checksumSha256: sha,
                fileSizeBytes: json.Length,
                manifestJson: json);
            _dbContext.AuditCompliancePackages.Add(auditPkg);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // 9. Phase 17: AI Predictive Maintenance Digital Twins
        var subHealthCount = await _dbContext.VehicleSubsystemHealths.CountAsync(h => h.TenantId == tenantId && h.VehicleId == volvoVehicle.Id, cancellationToken);
        if (subHealthCount == 0)
        {
            var h1 = new VehicleSubsystemHealth(tenantId, volvoVehicle.Id, SubsystemCategory.PowertrainEngine, 82m, HealthTrend.Stable, 0, 180, "Cylinder compression nominal, cooling pressure normal.");
            var h2 = new VehicleSubsystemHealth(tenantId, volvoVehicle.Id, SubsystemCategory.ElectricalBattery, 45m, HealthTrend.Degrading, 2, 4, "Alternator AC ripple detected > 0.45V, voltage drops to 9.1V on cold cranking.");
            var h3 = new VehicleSubsystemHealth(tenantId, volvoVehicle.Id, SubsystemCategory.BrakingChassis, 91m, HealthTrend.Improving, 0, 320, "Air brake reservoir holding pressure, pads 78% remaining.");
            var h4 = new VehicleSubsystemHealth(tenantId, volvoVehicle.Id, SubsystemCategory.TransmissionDrivetrain, 88m, HealthTrend.Stable, 0, 240, "I-Shift torque lockup within factory tolerance.");
            var h5 = new VehicleSubsystemHealth(tenantId, volvoVehicle.Id, SubsystemCategory.TiresSuspension, 79m, HealthTrend.Degrading, 1, 90, "Right steer tire showing 2.8mm accelerated shoulder wear.");

            _dbContext.VehicleSubsystemHealths.AddRange(h1, h2, h3, h4, h5);

            var alert = new PredictiveMaintenanceAlert(
                tenantId: tenantId,
                vehicleId: volvoVehicle.Id,
                subsystem: SubsystemCategory.ElectricalBattery,
                riskLevel: PredictiveRiskLevel.CriticalFailureImminent,
                componentTitle: "Alternator Diode Ripple & Cranking Voltage Sag",
                symptomDescription: "Telemetry indicates excessive AC ripple (0.48V) and severe starter voltage drop (9.1V), forecasting complete starter lockout.",
                recommendedAction: "Replace 160A high-output alternator and replace starting battery bank.",
                failureProbability: 88.5m,
                estimatedRulDays: 4,
                estimatedRepairCost: 850m,
                currency: "USD");

            _dbContext.PredictiveMaintenanceAlerts.Add(alert);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Master Enterprise Demo Seeding completed successfully for {TenantName}.", tenant.Name);

        return new EnterpriseDemoSeedingResultDto(
            TenantId: tenantId,
            TenantName: tenant.Name,
            VehiclesCreated: 4,
            EvStationsCreated: 3,
            ChargingSessionsCreated: 2,
            VrpRunsCreated: 1,
            SubsystemsEvaluated: 5,
            PredictiveAlertsCreated: 1,
            BrandingConfigured: true,
            CustomDomainRegistered: "fleet.apexlogistics.com",
            AuditPackageNumber: "AUDIT-SOC2-APEX-01",
            Message: $"Successfully seeded full enterprise operations dataset across all 20 phases for '{tenant.Name}'.");
    }
}
