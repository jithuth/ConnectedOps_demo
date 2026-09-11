using ConnectedOps.Domain.FleetOperations;

namespace ConnectedOps.Application.FleetOperations;

public sealed record FleetOperationsDashboardDto(
    int TotalFleet,
    int AvailableVehicles,
    int CheckedOutVehicles,
    int AssignedVehicles,
    int UnavailableVehicles,
    int TotalDrivers,
    int AvailableDrivers,
    int AssignedDrivers,
    int UnavailableDrivers,
    int OpenSessions,
    int CompletedSessionsToday,
    int CheckoutsToday,
    int CheckInsToday,
    int HandoversToday,
    int OpenOperationalExceptions,
    IReadOnlyDictionary<string, int> VehiclesByOperationalStatus,
    IReadOnlyDictionary<string, int> DriversByOperationalStatus,
    IReadOnlyDictionary<string, int> OperationsByBranch,
    IReadOnlyCollection<VehicleUsageSessionDto> RecentCheckouts,
    IReadOnlyCollection<VehicleUsageSessionDto> RecentCheckIns,
    IReadOnlyCollection<VehicleHandoverDto> RecentHandovers,
    IReadOnlyCollection<FleetOperationalExceptionDto> OpenExceptions);

public sealed record FleetOperationsBoardDto(
    int AvailableVehiclesCount,
    int CheckedOutVehiclesCount,
    int AssignedVehiclesCount,
    int UnavailableVehiclesCount,
    int AvailableDriversCount,
    int OpenExceptionsCount,
    IReadOnlyCollection<FleetOperationsBoardItemDto> Items);

public sealed record FleetOperationsBoardItemDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string DisplayName,
    string? MakeModel,
    string? BranchName,
    VehicleAvailabilityStatus AvailabilityStatus,
    string AvailabilityStatusName,
    string LifecycleStatusName,
    Guid? CurrentDriverId,
    string? CurrentDriverNumber,
    string? CurrentDriverName,
    Guid? CurrentShiftId,
    string? CurrentShiftName,
    Guid? CurrentSessionId,
    DateTime? CheckedOutAtUtc,
    decimal CurrentOdometer,
    bool HasOpenException);
