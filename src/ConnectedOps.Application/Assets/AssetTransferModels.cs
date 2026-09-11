using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetTransferDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    AssetTransferType TransferType,
    AssetTransferStatus Status,
    Guid? FromBranchId,
    string? FromBranchName,
    Guid? ToBranchId,
    string? ToBranchName,
    Guid? FromLocationId,
    string? FromLocationName,
    Guid? ToLocationId,
    string? ToLocationName,
    Guid? FromEmployeeId,
    string? FromEmployeeName,
    Guid? ToEmployeeId,
    string? ToEmployeeName,
    Guid? FromVehicleId,
    string? FromVehiclePlate,
    Guid? ToVehicleId,
    string? ToVehiclePlate,
    DateTime InitiatedAtUtc,
    string? InitiatedByUserId,
    string? InitiatedByUserName,
    DateTime? CompletedAtUtc,
    string? CompletedByUserId,
    string? CompletedByUserName,
    string? Reason,
    string? Notes);

public sealed record CreateAssetTransferRequest(
    Guid AssetId,
    AssetTransferType TransferType,
    Guid? TargetBranchId = null,
    Guid? TargetLocationId = null,
    Guid? TargetEmployeeId = null,
    Guid? TargetVehicleId = null,
    string? Reason = null,
    string? Notes = null);

public sealed record CompleteAssetTransferRequest(
    string? Notes = null);

public sealed record CancelAssetTransferRequest(
    string? Reason = null);

public sealed record AssetTransferFilter(
    Guid? AssetId = null,
    AssetTransferType? TransferType = null,
    AssetTransferStatus? Status = null,
    Guid? BranchId = null,
    Guid? EmployeeId = null,
    Guid? VehicleId = null,
    int PageNumber = 1,
    int PageSize = 20);
