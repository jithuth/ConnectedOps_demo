using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Drivers;

public sealed record DriverVehicleAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid DriverId,
    string DriverNumber,
    string DriverDisplayName,
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    string? RegistrationNumber,
    AssignmentType AssignmentType,
    string AssignmentTypeName,
    DateTime AssignedFromUtc,
    DateTime? AssignedToUtc,
    bool IsPrimary,
    bool IsActive,
    Guid? AssignedByUserId,
    Guid? EndedByUserId,
    string? Reason,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateDriverVehicleAssignmentRequest(
    Guid DriverId,
    Guid VehicleId,
    AssignmentType AssignmentType = AssignmentType.Primary,
    DateTime? AssignedFromUtc = null,
    DateTime? AssignedToUtc = null,
    bool IsPrimary = true,
    string? Reason = null,
    string? Notes = null);

public sealed record EndDriverVehicleAssignmentRequest(
    string? Reason = null,
    DateTime? EndedAtUtc = null);

public sealed record DriverEligibilityResult(
    bool IsEligible,
    IReadOnlyList<string> Reasons,
    Guid DriverId,
    string DriverName,
    Guid VehicleId,
    string VehicleNumber);
