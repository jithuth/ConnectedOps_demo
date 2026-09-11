namespace ConnectedOps.Application.Maintenance;

public sealed record MaintenanceDashboardDto(
    int TotalVehicles,
    int VehiclesWithUpcomingMaintenance,
    int VehiclesDueForMaintenance,
    int VehiclesOverdueForMaintenance,
    int VehiclesCurrentlyUnderMaintenance,
    int ServicesCompletedToday,
    int ServicesCompletedThisMonth,
    decimal MaintenanceCostThisMonth,
    decimal MaintenanceCostThisYear,
    decimal AverageDowntimeHours,
    IReadOnlyCollection<MaintenanceServiceTypeCountDto> UpcomingByServiceType,
    IReadOnlyCollection<MaintenanceBranchCountDto> OverdueByBranch,
    IReadOnlyCollection<RecentMaintenanceRecordDto> RecentCompletedMaintenance,
    IReadOnlyCollection<TopMaintenanceCostVehicleDto> HighestMaintenanceCostVehicles);

public sealed record MaintenanceServiceTypeCountDto(
    Guid ServiceTypeId,
    string ServiceTypeName,
    string CategoryName,
    int Count);

public sealed record MaintenanceBranchCountDto(
    Guid? BranchId,
    string BranchName,
    int Count);

public sealed record RecentMaintenanceRecordDto(
    Guid Id,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string ServiceTypeName,
    DateTime CompletedDateUtc,
    decimal TotalCost,
    string CurrencyCode,
    string? ProviderName);

public sealed record TopMaintenanceCostVehicleDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    string CategoryName,
    decimal TotalCost,
    int ServiceCount);
