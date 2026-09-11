namespace ConnectedOps.Application.Fuel;

public sealed record FuelDashboardDto(
    int FuelTransactionsToday,
    int FuelTransactionsThisMonth,
    decimal FuelQuantityTodayLiters,
    decimal FuelQuantityThisMonthLiters,
    decimal FuelCostToday,
    decimal FuelCostThisMonth,
    decimal FuelCostThisYear,
    string CurrencyCode,
    decimal AverageFuelPrice,
    decimal? AverageFleetEfficiencyKmPerLiter,
    decimal? AverageFleetEfficiencyLitersPer100Km,
    int VehiclesWithPoorEfficiencyCount,
    int OpenFuelAnomaliesCount,
    IReadOnlyCollection<TopFuelVehicleDto> TopFuelConsumingVehicles,
    IReadOnlyCollection<TopFuelVehicleDto> HighestFuelCostVehicles,
    IReadOnlyCollection<FuelByBranchDto> FuelByBranch,
    IReadOnlyCollection<FuelByTypeDto> FuelByType,
    IReadOnlyCollection<FuelTrendPointDto> MonthlyCostTrend,
    IReadOnlyCollection<FuelTransactionListItemDto> RecentTransactions,
    IReadOnlyCollection<FuelAnomalyDto> RecentAnomalies);

public sealed record TopFuelVehicleDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string DisplayName,
    string? CategoryName,
    decimal TotalQuantityLiters,
    decimal TotalCost,
    decimal? AverageKmPerLiter,
    int TransactionCount);
