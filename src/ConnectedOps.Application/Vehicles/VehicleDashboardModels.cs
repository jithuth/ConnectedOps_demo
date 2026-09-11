namespace ConnectedOps.Application.Vehicles;

public sealed record VehicleDashboardStatsDto(
    int TotalVehicles,
    int ActiveVehicles,
    int InServiceVehicles,
    int OutOfServiceVehicles,
    int UnderMaintenanceVehicles,
    int ReservedVehicles,
    int InactiveVehicles,
    decimal TotalOdometerKm,
    int ExpiringDocumentsCount,
    IReadOnlyDictionary<string, int> VehiclesByCategory,
    IReadOnlyDictionary<string, int> VehiclesByBranch,
    IReadOnlyDictionary<string, int> VehiclesByFuelType,
    IReadOnlyDictionary<string, int> VehiclesByOwnershipType,
    IReadOnlyCollection<VehicleListItemDto> RecentVehicles,
    IReadOnlyCollection<ExpiringVehicleDocumentAlertDto> CriticalDocumentAlerts);

public sealed record ExpiringVehicleDocumentAlertDto(
    Guid DocumentId,
    Guid VehicleId,
    string VehicleNumber,
    string DocumentTitle,
    string DocumentType,
    DateOnly? ExpiryDate,
    int DaysRemaining,
    bool IsExpired);
