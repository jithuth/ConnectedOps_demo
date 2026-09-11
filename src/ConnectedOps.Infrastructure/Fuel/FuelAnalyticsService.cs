using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelAnalyticsService : IFuelAnalyticsService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFuelEfficiencyService _efficiencyService;

    public FuelAnalyticsService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFuelEfficiencyService efficiencyService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _efficiencyService = efficiencyService;
    }

    public async Task<FuelStatisticsDto> GetFleetFuelStatisticsAsync(
        Guid? branchId = null,
        Guid? vehicleId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelTransactions
            .AsNoTracking()
            .Include(x => x.Vehicle)
                .ThenInclude(v => v.Branch)
            .Where(x => x.TenantId == tenantId && x.Status != FuelTransactionStatus.Cancelled);

        if (branchId.HasValue)
        {
            query = query.Where(x => x.Vehicle.BranchId == branchId.Value);
        }

        if (vehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == vehicleId.Value);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc <= toUtc.Value);
        }

        var transactions = await query
            .OrderBy(x => x.TransactionDateUtc)
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return new FuelStatisticsDto(
                0, 0, "USD", 0, 0, null, null, null, 0, 0,
                Array.Empty<FuelTrendPointDto>(),
                Array.Empty<FuelByBranchDto>(),
                Array.Empty<FuelByTypeDto>());
        }

        var totalQtyLiters = transactions.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
        var totalCost = transactions.Sum(x => x.TotalCost);
        var avgUnitPrice = transactions.Average(x => x.UnitPrice);
        var currency = transactions.FirstOrDefault()?.CurrencyCode ?? "USD";
        var fullTankCount = transactions.Count(x => x.IsFullTank);

        // Calculate vehicle intervals for accurate efficiency aggregation
        var vehicleGroups = transactions.GroupBy(x => x.VehicleId);
        decimal totalIntervalDistanceKm = 0;
        decimal totalIntervalVolumeLiters = 0;
        decimal totalIntervalCost = 0;

        foreach (var vGroup in vehicleGroups)
        {
            var vTransactions = vGroup.OrderBy(x => x.TransactionDateUtc).ToList();
            FuelTransaction? lastFull = null;
            var partials = new List<FuelTransaction>();

            foreach (var tx in vTransactions)
            {
                if (tx.IsFullTank)
                {
                    if (lastFull != null && tx.OdometerReading.HasValue && lastFull.OdometerReading.HasValue)
                    {
                        var eff = await _efficiencyService.CalculateIntervalEfficiencyAsync(tx, lastFull, partials, cancellationToken);
                        if (eff.HasSufficientData && eff.DistanceKilometers.HasValue && eff.VolumeLiters.HasValue && eff.DistanceKilometers.Value > 0)
                        {
                            totalIntervalDistanceKm += eff.DistanceKilometers.Value;
                            totalIntervalVolumeLiters += eff.VolumeLiters.Value;
                            totalIntervalCost += (tx.TotalCost + partials.Sum(p => p.TotalCost));
                        }
                    }
                    lastFull = tx;
                    partials.Clear();
                }
                else
                {
                    partials.Add(tx);
                }
            }
        }

        decimal? avgFleetKmL = FuelUnitConverter.CalculateKilometersPerLiter(totalIntervalDistanceKm, totalIntervalVolumeLiters);
        decimal? avgFleetL100 = FuelUnitConverter.CalculateLitersPer100Km(totalIntervalDistanceKm, totalIntervalVolumeLiters);
        decimal? avgCostPerKm = FuelUnitConverter.CalculateCostPerKilometer(totalIntervalCost, totalIntervalDistanceKm);

        // Group trends by month
        var trends = transactions
            .GroupBy(x => new { x.TransactionDateUtc.Year, x.TransactionDateUtc.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var monthDate = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                var monthQty = g.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
                var monthCost = g.Sum(x => x.TotalCost);
                return new FuelTrendPointDto(
                    monthDate,
                    monthDate.ToString("MMM yyyy"),
                    Math.Round(monthQty, 2),
                    Math.Round(monthCost, 2),
                    null,
                    null,
                    g.Count());
            })
            .ToList();

        // Group by branch
        var byBranch = transactions
            .GroupBy(x => new { x.Vehicle?.BranchId, BranchName = x.Vehicle?.Branch?.Name ?? "Unassigned" })
            .Select(g => new FuelByBranchDto(
                g.Key.BranchId,
                g.Key.BranchName,
                Math.Round(g.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit)), 2),
                Math.Round(g.Sum(x => x.TotalCost), 2),
                g.Count()))
            .OrderByDescending(b => b.TotalCost)
            .ToList();

        // Group by fuel type
        var byFuelType = transactions
            .GroupBy(x => x.FuelType)
            .Select(g => new FuelByTypeDto(
                g.Key,
                g.Key.ToString(),
                Math.Round(g.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit)), 2),
                Math.Round(g.Sum(x => x.TotalCost), 2),
                g.Count()))
            .OrderByDescending(f => f.TotalCost)
            .ToList();

        return new FuelStatisticsDto(
            Math.Round(totalQtyLiters, 2),
            Math.Round(totalCost, 2),
            currency,
            Math.Round(avgUnitPrice, 3),
            Math.Round(totalIntervalDistanceKm, 2),
            avgFleetKmL,
            avgFleetL100,
            avgCostPerKm,
            transactions.Count,
            fullTankCount,
            trends,
            byBranch,
            byFuelType);
    }

    public async Task<VehicleFuelSummaryDto> GetVehicleFuelSummaryAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var transactions = await _dbContext.FuelTransactions
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.TenantId == tenantId && x.Status != FuelTransactionStatus.Cancelled)
            .OrderBy(x => x.TransactionDateUtc)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var thisMonthTransactions = transactions
            .Where(x => x.TransactionDateUtc.Year == now.Year && x.TransactionDateUtc.Month == now.Month)
            .ToList();

        var fuelThisMonthLiters = thisMonthTransactions.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
        var costThisMonth = thisMonthTransactions.Sum(x => x.TotalCost);
        var currency = transactions.LastOrDefault()?.CurrencyCode ?? "USD";

        var lastTx = transactions.LastOrDefault();

        // Calculate efficiency over all vehicle transactions
        decimal totalDistanceKm = 0;
        decimal totalVolumeLiters = 0;
        decimal totalCost = 0;

        FuelTransaction? lastFull = null;
        var partials = new List<FuelTransaction>();

        foreach (var tx in transactions)
        {
            if (tx.IsFullTank)
            {
                if (lastFull != null && tx.OdometerReading.HasValue && lastFull.OdometerReading.HasValue)
                {
                    var eff = await _efficiencyService.CalculateIntervalEfficiencyAsync(tx, lastFull, partials, cancellationToken);
                    if (eff.HasSufficientData && eff.DistanceKilometers.HasValue && eff.VolumeLiters.HasValue && eff.DistanceKilometers.Value > 0)
                    {
                        totalDistanceKm += eff.DistanceKilometers.Value;
                        totalVolumeLiters += eff.VolumeLiters.Value;
                        totalCost += (tx.TotalCost + partials.Sum(p => p.TotalCost));
                    }
                }
                lastFull = tx;
                partials.Clear();
            }
            else
            {
                partials.Add(tx);
            }
        }

        var openAnomalies = await _dbContext.FuelAnomalies
            .CountAsync(x => x.VehicleId == vehicleId && x.TenantId == tenantId && x.Status == FuelAnomalyStatus.Open, cancellationToken);

        return new VehicleFuelSummaryDto(
            vehicle.Id,
            vehicle.VehicleNumber,
            vehicle.RegistrationNumber,
            vehicle.DisplayName,
            lastTx?.TransactionDateUtc,
            lastTx != null ? FuelUnitConverter.ToLiters(lastTx.Quantity, lastTx.QuantityUnit) : null,
            lastTx?.OdometerReading,
            Math.Round(fuelThisMonthLiters, 2),
            Math.Round(costThisMonth, 2),
            currency,
            FuelUnitConverter.CalculateKilometersPerLiter(totalDistanceKm, totalVolumeLiters),
            FuelUnitConverter.CalculateLitersPer100Km(totalDistanceKm, totalVolumeLiters),
            FuelUnitConverter.CalculateCostPerKilometer(totalCost, totalDistanceKm),
            openAnomalies,
            transactions.Count);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
