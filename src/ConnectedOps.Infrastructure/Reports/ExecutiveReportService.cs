using System.Globalization;
using System.Text;
using System.Text.Json;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Reports;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Reports;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Reports;

public sealed class ExecutiveReportService : IExecutiveReportService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    // Standard GHG emission factors (kg CO2e per unit: Liter or m3)
    private static readonly Dictionary<string, decimal> DefaultEmissionFactors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "DIESEL", 2.68m },
        { "GAS_REG", 2.31m },
        { "GAS_PREM", 2.35m },
        { "GASOLINE", 2.31m },
        { "BIODIESEL", 0.72m },
        { "ETHANOL_E85", 0.65m },
        { "CNG", 1.98m },
        { "LPG", 1.51m },
        { "ELECTRIC", 0.00m },
        { "HYDROGEN", 0.00m }
    };

    public ExecutiveReportService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid RequireTenantId()
    {
        if (_currentUserContext.TenantId is not { } tenantId || tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Active tenant context is required for reports.");
        }
        return tenantId;
    }

    public async Task<ExecutiveSummaryDto> GetExecutiveSummaryAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromDate, toDate) = NormalizeDateRange(filters.FromDateUtc, filters.ToDateUtc);

        // 1. Vehicles
        var vehiclesQuery = _dbContext.Vehicles
            .Where(v => v.TenantId == tenantId);

        if (filters.BranchId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.BranchId == filters.BranchId.Value);
        if (filters.VehicleCategoryId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.VehicleCategoryId == filters.VehicleCategoryId.Value);

        var vehicles = await vehiclesQuery
            .Select(v => new { v.Id, v.Status, v.VehicleCategoryId, v.CurrentOdometer })
            .ToListAsync(cancellationToken);

        var totalVehicles = vehicles.Count;
        var activeVehicles = vehicles.Count(v => v.Status == VehicleStatus.Active || v.Status == VehicleStatus.InService);
        var inMaintenanceVehicles = vehicles.Count(v => v.Status == VehicleStatus.UnderMaintenance || v.Status == VehicleStatus.OutOfService);
        var fleetAvailability = totalVehicles > 0
            ? Math.Round(((decimal)activeVehicles / totalVehicles) * 100m, 1)
            : 100m;

        var vehicleIds = vehicles.Select(v => v.Id).ToHashSet();

        // 2. Fuel Spend & Liters
        var fuelQuery = _dbContext.FuelTransactions
            .Where(f => f.TenantId == tenantId &&
                        f.TransactionDateUtc >= fromDate &&
                        f.TransactionDateUtc <= toDate &&
                        f.Status != FuelTransactionStatus.Cancelled);

        if (vehicleIds.Count > 0 && (filters.BranchId.HasValue || filters.VehicleCategoryId.HasValue))
        {
            fuelQuery = fuelQuery.Where(f => vehicleIds.Contains(f.VehicleId));
        }

        var fuelTxList = await fuelQuery
            .Select(f => new
            {
                f.TotalCost,
                f.Quantity,
                f.TransactionDateUtc,
                FuelTypeCode = f.FuelType.ToString()
            })
            .ToListAsync(cancellationToken);

        var totalFuelSpend = fuelTxList.Sum(f => f.TotalCost);
        var totalFuelVolume = fuelTxList.Sum(f => f.Quantity);

        // Carbon calculation
        decimal totalCo2Kg = 0m;
        foreach (var tx in fuelTxList)
        {
            var factor = GetEmissionFactor(tx.FuelTypeCode);
            totalCo2Kg += tx.Quantity * factor;
        }
        var totalCo2Tons = Math.Round(totalCo2Kg / 1000m, 2);

        // 3. Maintenance Spend
        var maintenanceQuery = _dbContext.VehicleMaintenanceRecords
            .Where(m => m.TenantId == tenantId &&
                        m.ServiceDateUtc >= fromDate &&
                        m.ServiceDateUtc <= toDate &&
                        m.Status != MaintenanceRecordStatus.Cancelled);

        if (vehicleIds.Count > 0 && (filters.BranchId.HasValue || filters.VehicleCategoryId.HasValue))
        {
            maintenanceQuery = maintenanceQuery.Where(m => vehicleIds.Contains(m.VehicleId));
        }

        var maintenanceList = await maintenanceQuery
            .Select(m => new { Cost = m.TotalCost ?? 0m, m.ServiceDateUtc })
            .ToListAsync(cancellationToken);

        var totalMaintenanceSpend = maintenanceList.Sum(m => m.Cost);

        // 4. Incidents & Violations
        var incidentsQuery = _dbContext.SafetyIncidents
            .Where(i => i.TenantId == tenantId &&
                        i.OccurredAtUtc >= fromDate &&
                        i.OccurredAtUtc <= toDate &&
                        i.Status != SafetyIncidentStatus.Cancelled);

        var incidentsList = await incidentsQuery
            .Select(i => new { i.Severity, i.OccurredAtUtc })
            .ToListAsync(cancellationToken);

        decimal totalIncidentCosts = incidentsList.Sum(i => EstimateIncidentCost(i.Severity));
        var totalIncidentsCount = incidentsList.Count;

        var violationsCount = await _dbContext.SafetyViolations
            .Where(v => v.TenantId == tenantId &&
                        v.OccurredAtUtc >= fromDate &&
                        v.OccurredAtUtc <= toDate)
            .CountAsync(cancellationToken);

        // 5. Total Distance & Cost / Km
        var totalDistanceKm = vehicles.Sum(v => v.CurrentOdometer);
        var totalOperatingCost = totalFuelSpend + totalMaintenanceSpend + totalIncidentCosts;
        var costPerKm = totalDistanceKm > 0 ? Math.Round(totalOperatingCost / totalDistanceKm, 2) : 0m;
        var avgCo2PerKmGrams = totalDistanceKm > 0 ? Math.Round((totalCo2Kg * 1000m) / totalDistanceKm, 1) : 0m;

        // 6. Monthly Spends Breakdown
        var monthlyMap = new Dictionary<string, (decimal Fuel, decimal Maint, decimal Inc)>();
        for (var d = fromDate; d <= toDate; d = d.AddMonths(1))
        {
            var key = d.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            monthlyMap[key] = (0m, 0m, 0m);
        }

        foreach (var f in fuelTxList)
        {
            var k = f.TransactionDateUtc.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            if (monthlyMap.TryGetValue(k, out var cur))
            {
                monthlyMap[k] = (cur.Fuel + f.TotalCost, cur.Maint, cur.Inc);
            }
            else
            {
                monthlyMap[k] = (f.TotalCost, 0m, 0m);
            }
        }

        foreach (var m in maintenanceList)
        {
            var k = m.ServiceDateUtc.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            if (monthlyMap.TryGetValue(k, out var cur))
            {
                monthlyMap[k] = (cur.Fuel, cur.Maint + m.Cost, cur.Inc);
            }
            else
            {
                monthlyMap[k] = (0m, m.Cost, 0m);
            }
        }

        foreach (var i in incidentsList)
        {
            var k = i.OccurredAtUtc.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var cost = EstimateIncidentCost(i.Severity);
            if (monthlyMap.TryGetValue(k, out var cur))
            {
                monthlyMap[k] = (cur.Fuel, cur.Maint, cur.Inc + cost);
            }
            else
            {
                monthlyMap[k] = (0m, 0m, cost);
            }
        }

        var monthlySpends = monthlyMap
            .OrderBy(kvp => kvp.Key)
            .TakeLast(12)
            .Select(kvp => new MonthlyFinancialSpendDto
            {
                YearMonth = kvp.Key,
                FuelSpend = Math.Round(kvp.Value.Fuel, 2),
                MaintenanceSpend = Math.Round(kvp.Value.Maint, 2),
                IncidentCost = Math.Round(kvp.Value.Inc, 2)
            })
            .ToList();

        // 7. Category Costs Breakdown
        var categories = await _dbContext.VehicleCategories
            .Where(c => c.TenantId == tenantId)
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var categoryCosts = vehicles
            .GroupBy(v => v.VehicleCategoryId)
            .Select(g =>
            {
                var catName = categories.TryGetValue(g.Key, out var name) ? name : "General Fleet";
                return new CategoryCostBreakdownDto
                {
                    CategoryName = catName,
                    VehicleCount = g.Count(),
                    TotalCost = totalVehicles > 0 ? Math.Round((totalOperatingCost * g.Count()) / totalVehicles, 2) : 0m
                };
            })
            .OrderByDescending(c => c.TotalCost)
            .ToList();

        return new ExecutiveSummaryDto
        {
            TotalFleetOperatingCost = Math.Round(totalOperatingCost, 2),
            CostPerKilometer = costPerKm,
            TotalDistanceKm = Math.Round(totalDistanceKm, 1),
            FleetAvailabilityPercent = fleetAvailability,
            TotalVehiclesCount = totalVehicles,
            ActiveVehiclesCount = activeVehicles,
            VehiclesInMaintenanceCount = inMaintenanceVehicles,
            TotalFuelSpend = Math.Round(totalFuelSpend, 2),
            TotalFuelLiters = Math.Round(totalFuelVolume, 1),
            TotalMaintenanceSpend = Math.Round(totalMaintenanceSpend, 2),
            TotalIncidentCosts = Math.Round(totalIncidentCosts, 2),
            TotalCo2MetricTons = totalCo2Tons,
            AverageCo2PerKmGrams = avgCo2PerKmGrams,
            TotalIncidentsCount = totalIncidentsCount,
            TotalViolationsCount = violationsCount,
            MonthlySpends = monthlySpends,
            CategoryCosts = categoryCosts
        };
    }

    public async Task<FleetTcoReportDto> GetFleetTcoReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromDate, toDate) = NormalizeDateRange(filters.FromDateUtc, filters.ToDateUtc);

        var query = _dbContext.Vehicles
            .Include(v => v.Branch)
            .Include(v => v.VehicleCategory)
            .Include(v => v.VehicleMake)
            .Include(v => v.VehicleModel)
            .Where(v => v.TenantId == tenantId);

        if (filters.BranchId.HasValue)
            query = query.Where(v => v.BranchId == filters.BranchId.Value);
        if (filters.VehicleCategoryId.HasValue)
            query = query.Where(v => v.VehicleCategoryId == filters.VehicleCategoryId.Value);
        if (filters.VehicleId.HasValue)
            query = query.Where(v => v.Id == filters.VehicleId.Value);

        if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
        {
            var term = filters.SearchTerm.Trim().ToLower();
            query = query.Where(v => (v.RegistrationNumber != null && v.RegistrationNumber.ToLower().Contains(term)) ||
                                     (v.VIN != null && v.VIN.ToLower().Contains(term)) ||
                                     v.VehicleNumber.ToLower().Contains(term) ||
                                     v.DisplayName.ToLower().Contains(term));
        }

        var vehicles = await query.ToListAsync(cancellationToken);
        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        // Sum Fuel per vehicle
        var fuelTotals = await _dbContext.FuelTransactions
            .Where(f => f.TenantId == tenantId &&
                        vehicleIds.Contains(f.VehicleId) &&
                        f.TransactionDateUtc >= fromDate &&
                        f.TransactionDateUtc <= toDate &&
                        f.Status != FuelTransactionStatus.Cancelled)
            .GroupBy(f => f.VehicleId)
            .Select(g => new { VehicleId = g.Key, Total = g.Sum(x => x.TotalCost) })
            .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

        // Sum Maintenance per vehicle
        var maintTotals = await _dbContext.VehicleMaintenanceRecords
            .Where(m => m.TenantId == tenantId &&
                        vehicleIds.Contains(m.VehicleId) &&
                        m.ServiceDateUtc >= fromDate &&
                        m.ServiceDateUtc <= toDate &&
                        m.Status != MaintenanceRecordStatus.Cancelled)
            .GroupBy(m => m.VehicleId)
            .Select(g => new { VehicleId = g.Key, Total = g.Sum(x => x.TotalCost ?? 0m) })
            .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

        // Incidents related to vehicles via SafetyIncidentVehicles
        var incidentTotals = await _dbContext.SafetyIncidents
            .Where(i => i.TenantId == tenantId &&
                        i.OccurredAtUtc >= fromDate &&
                        i.OccurredAtUtc <= toDate &&
                        i.Status != SafetyIncidentStatus.Cancelled)
            .SelectMany(i => i.Vehicles.Select(iv => new { iv.VehicleId, i.Severity }))
            .Where(iv => vehicleIds.Contains(iv.VehicleId))
            .GroupBy(iv => iv.VehicleId)
            .Select(g => new { VehicleId = g.Key, Total = g.Sum(x => EstimateIncidentCost(x.Severity)) })
            .ToDictionaryAsync(x => x.VehicleId, x => x.Total, cancellationToken);

        var items = new List<VehicleTcoItemDto>();
        decimal grandAcquisition = 0m;
        decimal grandFuel = 0m;
        decimal grandMaint = 0m;
        decimal grandIncidents = 0m;
        decimal grandOdometer = 0m;

        foreach (var v in vehicles)
        {
            var fuel = fuelTotals.GetValueOrDefault(v.Id, 0m);
            var maint = maintTotals.GetValueOrDefault(v.Id, 0m);
            var inc = incidentTotals.GetValueOrDefault(v.Id, 0m);
            var acq = v.PurchasePrice ?? 0m;

            grandAcquisition += acq;
            grandFuel += fuel;
            grandMaint += maint;
            grandIncidents += inc;
            grandOdometer += v.CurrentOdometer;

            items.Add(new VehicleTcoItemDto
            {
                VehicleId = v.Id,
                Vin = v.VIN ?? v.VehicleNumber,
                LicensePlate = v.RegistrationNumber ?? v.VehicleNumber,
                Make = v.VehicleMake?.Name ?? "Fleet",
                Model = v.VehicleModel?.Name ?? "Standard",
                Year = v.ModelYear ?? DateTime.UtcNow.Year,
                BranchName = v.Branch?.Name ?? "Main Depot",
                CategoryName = v.VehicleCategory?.Name ?? "Commercial",
                AcquisitionPrice = acq,
                FuelCost = Math.Round(fuel, 2),
                MaintenanceCost = Math.Round(maint, 2),
                IncidentCost = Math.Round(inc, 2),
                OdometerKm = Math.Round(v.CurrentOdometer, 1)
            });
        }

        var grandOperating = grandFuel + grandMaint + grandIncidents;
        var overallCostPerKm = grandOdometer > 0 ? Math.Round(grandOperating / grandOdometer, 2) : 0m;

        return new FleetTcoReportDto
        {
            GrandTotalTco = Math.Round(grandAcquisition + grandOperating, 2),
            GrandTotalAcquisition = Math.Round(grandAcquisition, 2),
            GrandTotalFuel = Math.Round(grandFuel, 2),
            GrandTotalMaintenance = Math.Round(grandMaint, 2),
            GrandTotalIncidents = Math.Round(grandIncidents, 2),
            OverallCostPerKm = overallCostPerKm,
            Vehicles = items.OrderByDescending(x => x.TotalTco).ToList()
        };
    }

    public async Task<FleetUtilizationReportDto> GetFleetUtilizationReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromDate, toDate) = NormalizeDateRange(filters.FromDateUtc, filters.ToDateUtc);

        var vehiclesQuery = _dbContext.Vehicles
            .Include(v => v.Branch)
            .Include(v => v.VehicleMake)
            .Include(v => v.VehicleModel)
            .Where(v => v.TenantId == tenantId);

        if (filters.BranchId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.BranchId == filters.BranchId.Value);
        if (filters.VehicleCategoryId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.VehicleCategoryId == filters.VehicleCategoryId.Value);

        var vehicles = await vehiclesQuery.ToListAsync(cancellationToken);
        var vehicleIds = vehicles.Select(v => v.Id).ToList();

        var sessions = await _dbContext.VehicleUsageSessions
            .Where(s => s.TenantId == tenantId &&
                        vehicleIds.Contains(s.VehicleId) &&
                        s.CheckedOutAtUtc >= fromDate &&
                        s.CheckedOutAtUtc <= toDate)
            .Select(s => new
            {
                s.VehicleId,
                s.CheckedOutAtUtc,
                s.CheckedInAtUtc,
                s.StartOdometer,
                s.EndOdometer
            })
            .ToListAsync(cancellationToken);

        var totalDays = Math.Max(1, (toDate - fromDate).TotalDays);
        var maxPossibleHoursPerVehicle = totalDays * 24.0;

        var vehicleItems = new List<VehicleUtilizationItemDto>();
        double totalFleetHours = 0;
        decimal totalFleetDistance = 0;

        var sessionsByVehicle = sessions.GroupBy(s => s.VehicleId).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var v in vehicles)
        {
            var vSessions = sessionsByVehicle.GetValueOrDefault(v.Id, []);
            double hours = 0;
            decimal distance = 0;

            foreach (var s in vSessions)
            {
                var end = s.CheckedInAtUtc ?? DateTime.UtcNow;
                var duration = Math.Max(0, (end - s.CheckedOutAtUtc).TotalHours);
                hours += duration;

                if (s.EndOdometer.HasValue && s.EndOdometer.Value >= s.StartOdometer)
                {
                    distance += (s.EndOdometer.Value - s.StartOdometer);
                }
            }

            totalFleetHours += hours;
            totalFleetDistance += distance;

            var utilizationRate = maxPossibleHoursPerVehicle > 0
                ? Math.Min(100m, Math.Round((decimal)(hours / maxPossibleHoursPerVehicle) * 100m, 1))
                : 0m;

            var makeModel = $"{v.VehicleMake?.Name} {v.VehicleModel?.Name}".Trim();
            if (string.IsNullOrWhiteSpace(makeModel)) makeModel = v.DisplayName;

            vehicleItems.Add(new VehicleUtilizationItemDto
            {
                VehicleId = v.Id,
                LicensePlate = v.RegistrationNumber ?? v.VehicleNumber,
                MakeModel = makeModel,
                BranchName = v.Branch?.Name ?? "Main Yard",
                TotalSessionsCount = vSessions.Count,
                TotalOperatingHours = Math.Round(hours, 1),
                DistanceTraveledKm = Math.Round(distance, 1),
                UtilizationRatePercent = utilizationRate,
                CurrentStatus = v.Status.ToString()
            });
        }

        // Day of week heatmap
        var dayNames = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
        var dayOfWeekHeatmap = new List<DayOfWeekUtilizationDto>();

        foreach (var day in dayNames)
        {
            var matches = sessions.Where(s => s.CheckedOutAtUtc.DayOfWeek == day).ToList();
            double hrs = 0;
            foreach (var s in matches)
            {
                var end = s.CheckedInAtUtc ?? DateTime.UtcNow;
                hrs += Math.Max(0, (end - s.CheckedOutAtUtc).TotalHours);
            }

            dayOfWeekHeatmap.Add(new DayOfWeekUtilizationDto
            {
                DayName = day.ToString(),
                SessionCount = matches.Count,
                HoursLogged = Math.Round(hrs, 1)
            });
        }

        var totalMaxHours = vehicles.Count * maxPossibleHoursPerVehicle;
        var overallUtilization = totalMaxHours > 0
            ? Math.Min(100m, Math.Round((decimal)(totalFleetHours / totalMaxHours) * 100m, 1))
            : 0m;

        return new FleetUtilizationReportDto
        {
            OverallFleetUtilizationRate = overallUtilization,
            TotalFleetOperatingHours = Math.Round(totalFleetHours, 1),
            TotalFleetDistanceKm = Math.Round(totalFleetDistance, 1),
            PeakActiveVehicles = vehicles.Count(v => v.Status == VehicleStatus.Active || v.Status == VehicleStatus.InService),
            DayOfWeekHeatmap = dayOfWeekHeatmap,
            VehicleUtilizations = vehicleItems.OrderByDescending(v => v.UtilizationRatePercent).ToList()
        };
    }

    public async Task<EsgCarbonReportDto> GetEsgCarbonReportAsync(
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var (fromDate, toDate) = NormalizeDateRange(filters.FromDateUtc, filters.ToDateUtc);

        var vehiclesQuery = _dbContext.Vehicles.Where(v => v.TenantId == tenantId);
        if (filters.BranchId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.BranchId == filters.BranchId.Value);
        if (filters.VehicleCategoryId.HasValue)
            vehiclesQuery = vehiclesQuery.Where(v => v.VehicleCategoryId == filters.VehicleCategoryId.Value);

        var vehicles = await vehiclesQuery
            .Select(v => new { v.Id, v.FuelType, v.CurrentOdometer })
            .ToListAsync(cancellationToken);

        var vehicleIds = vehicles.Select(v => v.Id).ToHashSet();

        var fuelQuery = _dbContext.FuelTransactions
            .Where(f => f.TenantId == tenantId &&
                        f.TransactionDateUtc >= fromDate &&
                        f.TransactionDateUtc <= toDate &&
                        f.Status != FuelTransactionStatus.Cancelled);

        if (vehicleIds.Count > 0 && (filters.BranchId.HasValue || filters.VehicleCategoryId.HasValue))
        {
            fuelQuery = fuelQuery.Where(f => vehicleIds.Contains(f.VehicleId));
        }

        var fuelList = await fuelQuery
            .Select(f => new
            {
                f.Quantity,
                f.TotalCost,
                f.TransactionDateUtc,
                FuelTypeCode = f.FuelType.ToString()
            })
            .ToListAsync(cancellationToken);

        var fuelGroups = fuelList.GroupBy(f => f.FuelTypeCode);
        var breakdown = new List<FuelEmissionBreakdownDto>();
        decimal totalCo2Kg = 0m;

        foreach (var g in fuelGroups)
        {
            var factor = GetEmissionFactor(g.Key);
            var vol = g.Sum(x => x.Quantity);
            var co2Kg = vol * factor;
            totalCo2Kg += co2Kg;

            breakdown.Add(new FuelEmissionBreakdownDto
            {
                FuelType = g.Key,
                TotalVolume = Math.Round(vol, 1),
                Unit = "Liters",
                Co2MetricTons = Math.Round(co2Kg / 1000m, 2),
                PercentageOfTotal = 0m
            });
        }

        var totalCo2Tons = Math.Round(totalCo2Kg / 1000m, 2);

        var formattedBreakdown = breakdown.Select(b => b with
        {
            PercentageOfTotal = totalCo2Tons > 0 ? Math.Round((b.Co2MetricTons / totalCo2Tons) * 100m, 1) : 0m
        }).OrderByDescending(b => b.Co2MetricTons).ToList();

        // Monthly trends
        var monthlyMap = new Dictionary<string, (decimal Co2Kg, decimal Distance)>();
        for (var d = fromDate; d <= toDate; d = d.AddMonths(1))
        {
            monthlyMap[d.ToString("yyyy-MM", CultureInfo.InvariantCulture)] = (0m, 0m);
        }

        foreach (var f in fuelList)
        {
            var key = f.TransactionDateUtc.ToString("yyyy-MM", CultureInfo.InvariantCulture);
            var factor = GetEmissionFactor(f.FuelTypeCode);
            var kg = f.Quantity * factor;

            if (monthlyMap.TryGetValue(key, out var val))
            {
                monthlyMap[key] = (val.Co2Kg + kg, val.Distance);
            }
            else
            {
                monthlyMap[key] = (kg, 0m);
            }
        }

        var monthlyTrends = monthlyMap
            .OrderBy(kvp => kvp.Key)
            .TakeLast(12)
            .Select(kvp => new MonthlyCarbonTrendDto
            {
                YearMonth = kvp.Key,
                Co2MetricTons = Math.Round(kvp.Value.Co2Kg / 1000m, 2),
                DistanceKm = 0m
            })
            .ToList();

        var totalEv = vehicles.Count(v => v.FuelType == FuelType.Electric);
        var totalHybrid = vehicles.Count(v => v.FuelType == FuelType.Hybrid);
        var totalCombustion = vehicles.Count(v => v.FuelType != FuelType.Electric && v.FuelType != FuelType.Hybrid);

        var totalFleet = Math.Max(1, vehicles.Count);
        var greenPercent = Math.Round(((decimal)(totalEv + (totalHybrid * 0.5)) / totalFleet) * 100m, 1);

        var combustionCo2Tons = formattedBreakdown
            .Where(f => !f.FuelType.Contains("ELECTRIC", StringComparison.OrdinalIgnoreCase))
            .Sum(f => f.Co2MetricTons);

        var potentialEvSavings = Math.Round(combustionCo2Tons * 0.75m, 2);
        var totalFuelSpend = fuelList.Sum(f => f.TotalCost);
        var estimatedCostSavings = Math.Round(totalFuelSpend * 0.60m, 2);

        var totalDistanceKm = vehicles.Sum(v => v.CurrentOdometer);
        var avgKgCo2PerKm = totalDistanceKm > 0 ? Math.Round(totalCo2Kg / totalDistanceKm, 3) : 0m;

        return new EsgCarbonReportDto
        {
            TotalCo2MetricTons = totalCo2Tons,
            DirectScope1FuelTons = totalCo2Tons,
            AverageKgCo2PerKm = avgKgCo2PerKm,
            FleetGreenIndexScore = greenPercent,
            TotalCombustionVehicles = totalCombustion,
            TotalElectricVehicles = totalEv,
            TotalHybridVehicles = totalHybrid,
            PotentialEvCo2SavingsMetricTons = potentialEvSavings,
            EstimatedEvFuelCostSavings = estimatedCostSavings,
            FuelBreakdown = formattedBreakdown,
            MonthlyTrends = monthlyTrends
        };
    }

    public async Task<List<ReportDefinition>> GetAvailableReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = _currentUserContext.TenantId;

        var reports = await _dbContext.ReportDefinitions
            .Where(r => (r.TenantId == tenantId || r.TenantId == null) && r.IsActive)
            .OrderBy(r => r.Category)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        if (reports.Count == 0)
        {
            reports = GetDefaultReportCatalog();
        }

        return reports;
    }

    public async Task<ReportExportResult> ExportReportCsvAsync(
        string reportType,
        ReportFilterParameters filters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var sb = new StringBuilder();
        var fileName = $"{reportType.ToLowerInvariant()}_report_{DateTime.UtcNow:yyyyMMdd_HHmm}.csv";
        long recordCount = 0;

        switch (reportType.ToUpperInvariant())
        {
            case "TCO":
            case "FLEET_TCO":
                var tco = await GetFleetTcoReportAsync(filters, cancellationToken);
                recordCount = tco.Vehicles.Count;
                sb.AppendLine("Vehicle ID,VIN,License Plate,Make,Model,Year,Branch,Category,Acquisition Price,Fuel Cost,Maintenance Cost,Incident Cost,Total Operating Cost,Total TCO,Odometer Km,Cost Per Km");
                foreach (var v in tco.Vehicles)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(v.VehicleId.ToString()),
                        EscapeCsv(v.Vin),
                        EscapeCsv(v.LicensePlate),
                        EscapeCsv(v.Make),
                        EscapeCsv(v.Model),
                        v.Year,
                        EscapeCsv(v.BranchName),
                        EscapeCsv(v.CategoryName),
                        v.AcquisitionPrice.ToString("F2", CultureInfo.InvariantCulture),
                        v.FuelCost.ToString("F2", CultureInfo.InvariantCulture),
                        v.MaintenanceCost.ToString("F2", CultureInfo.InvariantCulture),
                        v.IncidentCost.ToString("F2", CultureInfo.InvariantCulture),
                        v.TotalOperatingCost.ToString("F2", CultureInfo.InvariantCulture),
                        v.TotalTco.ToString("F2", CultureInfo.InvariantCulture),
                        v.OdometerKm.ToString("F1", CultureInfo.InvariantCulture),
                        v.CostPerKm.ToString("F2", CultureInfo.InvariantCulture)));
                }
                break;

            case "UTILIZATION":
            case "FLEET_UTILIZATION":
                var ut = await GetFleetUtilizationReportAsync(filters, cancellationToken);
                recordCount = ut.VehicleUtilizations.Count;
                sb.AppendLine("Vehicle ID,License Plate,Make Model,Branch,Sessions Count,Operating Hours,Distance Km,Utilization Rate %,Status");
                foreach (var u in ut.VehicleUtilizations)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(u.VehicleId.ToString()),
                        EscapeCsv(u.LicensePlate),
                        EscapeCsv(u.MakeModel),
                        EscapeCsv(u.BranchName),
                        u.TotalSessionsCount,
                        u.TotalOperatingHours.ToString("F1", CultureInfo.InvariantCulture),
                        u.DistanceTraveledKm.ToString("F1", CultureInfo.InvariantCulture),
                        u.UtilizationRatePercent.ToString("F1", CultureInfo.InvariantCulture),
                        EscapeCsv(u.CurrentStatus)));
                }
                break;

            case "ESG":
            case "CARBON":
            case "SUSTAINABILITY":
                var esg = await GetEsgCarbonReportAsync(filters, cancellationToken);
                recordCount = esg.FuelBreakdown.Count;
                sb.AppendLine("Fuel Type,Total Volume,Unit,CO2 Metric Tons,Percentage of Total");
                foreach (var f in esg.FuelBreakdown)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(f.FuelType),
                        f.TotalVolume.ToString("F1", CultureInfo.InvariantCulture),
                        EscapeCsv(f.Unit),
                        f.Co2MetricTons.ToString("F2", CultureInfo.InvariantCulture),
                        f.PercentageOfTotal.ToString("F1", CultureInfo.InvariantCulture)));
                }
                break;

            default:
                var summary = await GetExecutiveSummaryAsync(filters, cancellationToken);
                recordCount = summary.MonthlySpends.Count;
                sb.AppendLine("Metric,Value");
                sb.AppendLine($"Total Fleet Operating Cost,{summary.TotalFleetOperatingCost.ToString("F2", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Cost Per Km,{summary.CostPerKilometer.ToString("F2", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Fleet Availability %,{summary.FleetAvailabilityPercent.ToString("F1", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Total Distance Km,{summary.TotalDistanceKm.ToString("F1", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Total Vehicles Count,{summary.TotalVehiclesCount}");
                sb.AppendLine($"Active Vehicles Count,{summary.ActiveVehiclesCount}");
                sb.AppendLine($"Total Fuel Spend,{summary.TotalFuelSpend.ToString("F2", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Total Maintenance Spend,{summary.TotalMaintenanceSpend.ToString("F2", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Total Incident Cost,{summary.TotalIncidentCosts.ToString("F2", CultureInfo.InvariantCulture)}");
                sb.AppendLine($"Total CO2 (Metric Tons),{summary.TotalCo2MetricTons.ToString("F2", CultureInfo.InvariantCulture)}");
                break;
        }

        sw.Stop();

        try
        {
            var log = new ReportExecutionLog(
                tenantId,
                reportType.ToUpperInvariant(),
                $"{reportType} Export",
                ReportExportFormat.Csv,
                JsonSerializer.Serialize(filters),
                recordCount,
                sw.ElapsedMilliseconds,
                _currentUserContext.UserId,
                _currentUserContext.Email ?? "System User");

            _dbContext.ReportExecutionLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Ignore execution log failure
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();

        return new ReportExportResult
        {
            FileName = fileName,
            ContentType = "text/csv; charset=utf-8",
            FileBytes = bytes
        };
    }

    private static decimal EstimateIncidentCost(SafetyIncidentSeverity severity)
    {
        return severity switch
        {
            SafetyIncidentSeverity.Critical => 5000m,
            SafetyIncidentSeverity.High => 2500m,
            SafetyIncidentSeverity.Moderate => 1000m,
            SafetyIncidentSeverity.Low => 300m,
            _ => 0m
        };
    }

    private static decimal GetEmissionFactor(string fuelCode)
    {
        if (DefaultEmissionFactors.TryGetValue(fuelCode, out var factor))
            return factor;

        if (fuelCode.Contains("DIESEL", StringComparison.OrdinalIgnoreCase)) return 2.68m;
        if (fuelCode.Contains("GAS", StringComparison.OrdinalIgnoreCase) || fuelCode.Contains("PETROL", StringComparison.OrdinalIgnoreCase)) return 2.31m;
        if (fuelCode.Contains("CNG", StringComparison.OrdinalIgnoreCase)) return 1.98m;
        if (fuelCode.Contains("LPG", StringComparison.OrdinalIgnoreCase)) return 1.51m;
        if (fuelCode.Contains("ELECTRIC", StringComparison.OrdinalIgnoreCase)) return 0.00m;

        return 2.50m;
    }

    private static (DateTime FromDate, DateTime ToDate) NormalizeDateRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddMonths(-12);
        if (fromDate > toDate) fromDate = toDate.AddMonths(-1);
        return (fromDate, toDate);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static List<ReportDefinition> GetDefaultReportCatalog()
    {
        return
        [
            new(null, "FLEET_TCO", "Fleet Total Cost of Ownership (TCO)", ReportCategory.Financial, "Calculates capital acquisition plus operational spend (fuel, maintenance, incidents) on a per-vehicle and per-kilometer basis."),
            new(null, "EXECUTIVE_KPI", "Executive Fleet Performance Summary", ReportCategory.Executive, "Consolidated C-suite overview of total expenditure, fleet uptime, cost efficiency, safety index, and carbon footprint."),
            new(null, "UTILIZATION", "Asset & Fleet Utilization Analytics", ReportCategory.Operational, "Detailed analysis of operating hours, active vs idle ratios, and day-of-week demand heatmaps."),
            new(null, "ESG_CARBON", "ESG Scope 1 Carbon Emissions Report", ReportCategory.Sustainability, "Greenhouse Gas Protocol (GHG) calculations of direct fuel combustion emissions and EV transition potential."),
            new(null, "MAINT_COST", "Preventive vs Corrective Maintenance Analysis", ReportCategory.Financial, "Breaks down workshop labour, replacement parts, scheduled services, and unplanned breakdown expenses."),
            new(null, "DRIVER_RISK", "Driver Safety & Telematics Risk Index", ReportCategory.Safety, "Evaluates driver safety compliance, speeding incidents, harsh maneuvers, and accident histories.")
        ];
    }
}
