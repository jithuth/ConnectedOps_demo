using ConnectedOps.Domain.Reports;

namespace ConnectedOps.Application.Reports;

public sealed record ReportFilterParameters
{
    public DateTime? FromDateUtc { get; init; }
    public DateTime? ToDateUtc { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? VehicleCategoryId { get; init; }
    public Guid? VehicleId { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}

public sealed record ExecutiveSummaryDto
{
    public decimal TotalFleetOperatingCost { get; init; }
    public decimal CostPerKilometer { get; init; }
    public decimal TotalDistanceKm { get; init; }
    public decimal FleetAvailabilityPercent { get; init; }
    public int TotalVehiclesCount { get; init; }
    public int ActiveVehiclesCount { get; init; }
    public int VehiclesInMaintenanceCount { get; init; }
    public decimal TotalFuelSpend { get; init; }
    public decimal TotalFuelLiters { get; init; }
    public decimal TotalMaintenanceSpend { get; init; }
    public decimal TotalIncidentCosts { get; init; }
    public decimal TotalCo2MetricTons { get; init; }
    public decimal AverageCo2PerKmGrams { get; init; }
    public int TotalIncidentsCount { get; init; }
    public int TotalViolationsCount { get; init; }
    public List<MonthlyFinancialSpendDto> MonthlySpends { get; init; } = [];
    public List<CategoryCostBreakdownDto> CategoryCosts { get; init; } = [];
}

public sealed record MonthlyFinancialSpendDto
{
    public string YearMonth { get; init; } = string.Empty;
    public decimal FuelSpend { get; init; }
    public decimal MaintenanceSpend { get; init; }
    public decimal IncidentCost { get; init; }
    public decimal TotalSpend => FuelSpend + MaintenanceSpend + IncidentCost;
}

public sealed record CategoryCostBreakdownDto
{
    public string CategoryName { get; init; } = string.Empty;
    public decimal TotalCost { get; init; }
    public int VehicleCount { get; init; }
}

public sealed record VehicleTcoItemDto
{
    public Guid VehicleId { get; init; }
    public string Vin { get; init; } = string.Empty;
    public string LicensePlate { get; init; } = string.Empty;
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public string BranchName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public decimal AcquisitionPrice { get; init; }
    public decimal FuelCost { get; init; }
    public decimal MaintenanceCost { get; init; }
    public decimal IncidentCost { get; init; }
    public decimal TotalOperatingCost => FuelCost + MaintenanceCost + IncidentCost;
    public decimal TotalTco => AcquisitionPrice + TotalOperatingCost;
    public decimal OdometerKm { get; init; }
    public decimal CostPerKm => OdometerKm > 0 ? Math.Round(TotalOperatingCost / OdometerKm, 2) : 0;
}

public sealed record FleetTcoReportDto
{
    public decimal GrandTotalTco { get; init; }
    public decimal GrandTotalAcquisition { get; init; }
    public decimal GrandTotalFuel { get; init; }
    public decimal GrandTotalMaintenance { get; init; }
    public decimal GrandTotalIncidents { get; init; }
    public decimal OverallCostPerKm { get; init; }
    public List<VehicleTcoItemDto> Vehicles { get; init; } = [];
}

public sealed record VehicleUtilizationItemDto
{
    public Guid VehicleId { get; init; }
    public string LicensePlate { get; init; } = string.Empty;
    public string MakeModel { get; init; } = string.Empty;
    public string BranchName { get; init; } = string.Empty;
    public int TotalSessionsCount { get; init; }
    public double TotalOperatingHours { get; init; }
    public decimal DistanceTraveledKm { get; init; }
    public decimal UtilizationRatePercent { get; init; }
    public string CurrentStatus { get; init; } = string.Empty;
}

public sealed record FleetUtilizationReportDto
{
    public decimal OverallFleetUtilizationRate { get; init; }
    public double TotalFleetOperatingHours { get; init; }
    public decimal TotalFleetDistanceKm { get; init; }
    public int PeakActiveVehicles { get; init; }
    public List<DayOfWeekUtilizationDto> DayOfWeekHeatmap { get; init; } = [];
    public List<VehicleUtilizationItemDto> VehicleUtilizations { get; init; } = [];
}

public sealed record DayOfWeekUtilizationDto
{
    public string DayName { get; init; } = string.Empty;
    public int SessionCount { get; init; }
    public double HoursLogged { get; init; }
}

public sealed record EsgCarbonReportDto
{
    public decimal TotalCo2MetricTons { get; init; }
    public decimal DirectScope1FuelTons { get; init; }
    public decimal AverageKgCo2PerKm { get; init; }
    public decimal FleetGreenIndexScore { get; init; }
    public int TotalCombustionVehicles { get; init; }
    public int TotalElectricVehicles { get; init; }
    public int TotalHybridVehicles { get; init; }
    public decimal PotentialEvCo2SavingsMetricTons { get; init; }
    public decimal EstimatedEvFuelCostSavings { get; init; }
    public List<FuelEmissionBreakdownDto> FuelBreakdown { get; init; } = [];
    public List<MonthlyCarbonTrendDto> MonthlyTrends { get; init; } = [];
}

public sealed record FuelEmissionBreakdownDto
{
    public string FuelType { get; init; } = string.Empty;
    public decimal TotalVolume { get; init; }
    public string Unit { get; init; } = "Liters";
    public decimal Co2MetricTons { get; init; }
    public decimal PercentageOfTotal { get; init; }
}

public sealed record MonthlyCarbonTrendDto
{
    public string YearMonth { get; init; } = string.Empty;
    public decimal Co2MetricTons { get; init; }
    public decimal DistanceKm { get; init; }
    public decimal EmissionIntensityGramsPerKm => DistanceKm > 0 ? Math.Round((Co2MetricTons * 1_000_000m) / DistanceKm, 1) : 0;
}

public sealed record ReportExportResult
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/csv";
    public byte[] FileBytes { get; init; } = [];
}
