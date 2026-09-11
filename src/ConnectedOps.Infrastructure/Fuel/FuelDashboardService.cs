using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelDashboardService : IFuelDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFuelAnalyticsService _analyticsService;
    private readonly IFuelTransactionService _transactionService;
    private readonly IFuelAnomalyService _anomalyService;

    public FuelDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFuelAnalyticsService analyticsService,
        IFuelTransactionService transactionService,
        IFuelAnomalyService anomalyService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _analyticsService = analyticsService;
        _transactionService = transactionService;
        _anomalyService = anomalyService;
    }

    public async Task<FuelDashboardDto> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thisYearStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var allThisYearTransactions = await _dbContext.FuelTransactions
            .AsNoTracking()
            .Include(x => x.Vehicle)
                .ThenInclude(v => v.VehicleCategory)
            .Where(x => x.TenantId == tenantId &&
                        x.Status != FuelTransactionStatus.Cancelled &&
                        x.TransactionDateUtc >= thisYearStart)
            .ToListAsync(cancellationToken);

        var todayList = allThisYearTransactions.Where(x => x.TransactionDateUtc >= todayStart).ToList();
        var monthList = allThisYearTransactions.Where(x => x.TransactionDateUtc >= thisMonthStart).ToList();

        var todayTxCount = todayList.Count;
        var monthTxCount = monthList.Count;

        var todayQty = todayList.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
        var monthQty = monthList.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));

        var todayCost = todayList.Sum(x => x.TotalCost);
        var monthCost = monthList.Sum(x => x.TotalCost);
        var yearCost = allThisYearTransactions.Sum(x => x.TotalCost);

        var currency = allThisYearTransactions.LastOrDefault()?.CurrencyCode ?? "USD";
        var avgPrice = allThisYearTransactions.Count > 0 ? allThisYearTransactions.Average(x => x.UnitPrice) : 0;

        // Analytics summary
        var stats = await _analyticsService.GetFleetFuelStatisticsAsync(
            null, null, thisYearStart, now, cancellationToken);

        var openAnomaliesCount = await _dbContext.FuelAnomalies
            .CountAsync(x => x.TenantId == tenantId && x.Status == FuelAnomalyStatus.Open, cancellationToken);

        // Top vehicles by consumption (this month)
        var topVehiclesByQty = monthList
            .GroupBy(x => x.VehicleId)
            .Select(g =>
            {
                var sample = g.First().Vehicle;
                var totalL = g.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
                var totalC = g.Sum(x => x.TotalCost);
                return new TopFuelVehicleDto(
                    g.Key,
                    sample?.VehicleNumber ?? string.Empty,
                    sample?.RegistrationNumber,
                    sample?.DisplayName ?? string.Empty,
                    sample?.VehicleCategory?.Name,
                    Math.Round(totalL, 2),
                    Math.Round(totalC, 2),
                    null,
                    g.Count());
            })
            .OrderByDescending(x => x.TotalQuantityLiters)
            .Take(5)
            .ToList();

        // Top vehicles by cost (this month)
        var topVehiclesByCost = monthList
            .GroupBy(x => x.VehicleId)
            .Select(g =>
            {
                var sample = g.First().Vehicle;
                var totalL = g.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
                var totalC = g.Sum(x => x.TotalCost);
                return new TopFuelVehicleDto(
                    g.Key,
                    sample?.VehicleNumber ?? string.Empty,
                    sample?.RegistrationNumber,
                    sample?.DisplayName ?? string.Empty,
                    sample?.VehicleCategory?.Name,
                    Math.Round(totalL, 2),
                    Math.Round(totalC, 2),
                    null,
                    g.Count());
            })
            .OrderByDescending(x => x.TotalCost)
            .Take(5)
            .ToList();

        // Recent transactions
        var recentTxPaged = await _transactionService.GetTransactionsPagedAsync(
            new FuelTransactionQueryParameters { PageNumber = 1, PageSize = 6 }, cancellationToken);

        // Recent anomalies
        var recentAnomaliesPaged = await _anomalyService.GetAnomaliesPagedAsync(
            new FuelAnomalyQueryParameters { PageNumber = 1, PageSize = 5 }, cancellationToken);

        var vehiclesWithPoorEfficiencyCount = await _dbContext.FuelAnomalies
            .Where(x => x.TenantId == tenantId &&
                        x.AnomalyType == FuelAnomalyType.LowEfficiency &&
                        x.Status == FuelAnomalyStatus.Open)
            .Select(x => x.VehicleId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new FuelDashboardDto(
            todayTxCount,
            monthTxCount,
            Math.Round(todayQty, 2),
            Math.Round(monthQty, 2),
            Math.Round(todayCost, 2),
            Math.Round(monthCost, 2),
            Math.Round(yearCost, 2),
            currency,
            Math.Round(avgPrice, 3),
            stats.AverageFleetEfficiencyKmPerLiter,
            stats.AverageFleetEfficiencyLitersPer100Km,
            vehiclesWithPoorEfficiencyCount,
            openAnomaliesCount,
            topVehiclesByQty,
            topVehiclesByCost,
            stats.ByBranch,
            stats.ByFuelType,
            stats.Trends,
            recentTxPaged.Items,
            recentAnomaliesPaged.Items);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
