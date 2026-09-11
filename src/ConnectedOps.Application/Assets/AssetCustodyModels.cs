using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetEmployeeAssignmentDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    Guid EmployeeId,
    string? EmployeeName,
    string? EmployeeCode,
    DateTime AssignedAtUtc,
    string? AssignedByUserId,
    string? AssignedByUserName,
    AssetCondition ConditionAtAssignment,
    DateTime? ExpectedReturnDateUtc,
    DateTime? ReturnedAtUtc,
    string? ReceivedByUserId,
    string? ReceivedByUserName,
    AssetCondition? ConditionAtReturn,
    bool IsActive,
    string? Notes);

public sealed record AssignAssetToEmployeeRequest(
    Guid AssetId,
    Guid EmployeeId,
    AssetCondition ConditionAtAssignment = AssetCondition.Good,
    DateTime? ExpectedReturnDateUtc = null,
    string? Notes = null);

public sealed record ReturnAssetFromEmployeeRequest(
    AssetCondition ConditionAtReturn,
    string? Notes = null);

public sealed record AssetVehicleAssignmentDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    Guid VehicleId,
    string? VehiclePlate,
    string? VehicleMakeModel,
    DateTime AssignedAtUtc,
    string? AssignedByUserId,
    string? AssignedByUserName,
    DateTime? RemovedAtUtc,
    string? RemovedByUserId,
    string? RemovedByUserName,
    bool IsActive,
    string? Notes);

public sealed record AssignAssetToVehicleRequest(
    Guid AssetId,
    Guid VehicleId,
    string? Notes = null);

public sealed record RemoveAssetFromVehicleRequest(
    string? Notes = null);

public sealed record AssetUsageSessionDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    Guid EmployeeId,
    string? EmployeeName,
    Guid? VehicleId,
    string? VehiclePlate,
    DateTime CheckedOutAtUtc,
    string? CheckedOutByUserId,
    string? CheckedOutByUserName,
    decimal? CheckoutMeterHours,
    AssetCondition ConditionAtCheckout,
    DateTime? ExpectedReturnUtc,
    DateTime? CheckedInAtUtc,
    string? CheckedInByUserId,
    string? CheckedInByUserName,
    decimal? CheckinMeterHours,
    AssetCondition? ConditionAtCheckin,
    AssetUsageSessionStatus Status,
    string? Purpose,
    string? Notes,
    decimal? DurationHours);

public sealed record StartAssetUsageSessionRequest(
    Guid AssetId,
    Guid EmployeeId,
    Guid? VehicleId = null,
    decimal? MeterHours = null,
    AssetCondition Condition = AssetCondition.Good,
    DateTime? ExpectedReturnUtc = null,
    string? Purpose = null,
    string? Notes = null);

public sealed record EndAssetUsageSessionRequest(
    decimal? MeterHours = null,
    AssetCondition Condition = AssetCondition.Good,
    string? Notes = null);
