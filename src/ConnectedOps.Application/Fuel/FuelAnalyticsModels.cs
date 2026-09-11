using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelStatisticsDto(
    decimal TotalQuantityLiters,
    decimal TotalCost,
    string CurrencyCode,
    decimal AverageUnitPrice,
    decimal TotalDistanceTravelledKm,
    decimal? AverageFleetEfficiencyKmPerLiter,
    decimal? AverageFleetEfficiencyLitersPer100Km,
    decimal? AverageCostPerKilometer,
    int TransactionCount,
    int FullTankCount,
    IReadOnlyCollection<FuelTrendPointDto> Trends,
    IReadOnlyCollection<FuelByBranchDto> ByBranch,
    IReadOnlyCollection<FuelByTypeDto> ByFuelType);

public sealed record FuelTrendPointDto(
    DateTime Date,
    string PeriodLabel,
    decimal QuantityLiters,
    decimal TotalCost,
    decimal? AverageKmPerLiter,
    decimal? AverageLitersPer100Km,
    int TransactionCount);

public sealed record FuelByBranchDto(
    Guid? BranchId,
    string BranchName,
    decimal TotalQuantityLiters,
    decimal TotalCost,
    int TransactionCount);

public sealed record FuelByTypeDto(
    FuelType FuelType,
    string FuelTypeName,
    decimal TotalQuantityLiters,
    decimal TotalCost,
    int TransactionCount);

public sealed record VehicleFuelSummaryDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string DisplayName,
    DateTime? LastFuelingDateUtc,
    decimal? LastQuantityLiters,
    decimal? LastOdometer,
    decimal FuelQuantityThisMonthLiters,
    decimal FuelCostThisMonth,
    string CurrencyCode,
    decimal? AverageEfficiencyKmPerLiter,
    decimal? AverageEfficiencyLitersPer100Km,
    decimal? AverageCostPerKilometer,
    int OpenAnomalyCount,
    int TotalTransactions);
