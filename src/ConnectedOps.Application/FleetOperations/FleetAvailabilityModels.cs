using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.FleetOperations;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public sealed record VehicleAvailabilityDto(
    Guid VehicleId,
    string VehicleNumber,
    string DisplayName,
    string? RegistrationNumber,
    Guid? BranchId,
    string? BranchName,
    VehicleStatus LifecycleStatus,
    VehicleAvailabilityStatus AvailabilityStatus,
    string AvailabilityStatusName,
    bool IsAvailable,
    string Reason,
    Guid? CurrentDriverId,
    string? CurrentDriverName,
    Guid? CurrentShiftId,
    string? CurrentShiftName,
    Guid? CurrentSessionId,
    DateTime? CheckedOutAtUtc,
    decimal CurrentOdometer);

public sealed record DriverAvailabilityResultDto(
    Guid DriverId,
    string DriverNumber,
    string DisplayName,
    Guid? BranchId,
    string? BranchName,
    DriverStatus LifecycleStatus,
    DriverAvailabilityStatus AvailabilityStatus,
    string AvailabilityStatusName,
    bool IsAvailable,
    string Reason,
    Guid? CurrentVehicleId,
    string? CurrentVehicleNumber,
    Guid? CurrentSessionId,
    DateTime? CheckedOutAtUtc);

public sealed record FleetAvailabilitySummaryDto(
    int TotalVehicles,
    int AvailableVehicles,
    int AssignedVehicles,
    int CheckedOutVehicles,
    int ReservedVehicles,
    int UnavailableVehicles,
    int TotalDrivers,
    int AvailableDrivers,
    int AssignedDrivers,
    int UnavailableDrivers,
    IReadOnlyCollection<VehicleAvailabilityDto> Vehicles,
    IReadOnlyCollection<DriverAvailabilityResultDto> Drivers);

public sealed class FleetAvailabilityFilter
{
    public Guid? BranchId { get; set; }
    public VehicleAvailabilityStatus? VehicleStatus { get; set; }
    public DriverAvailabilityStatus? DriverStatus { get; set; }
    public string? SearchTerm { get; set; }
}
