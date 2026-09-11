using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetIdentifierDto(
    Guid Id,
    Guid AssetId,
    AssetIdentifierType IdentifierType,
    string IdentifierValue,
    string PublicToken,
    bool IsPrimary,
    bool IsActive,
    DateTime CreatedAtUtc);

public sealed record CreateAssetIdentifierRequest(
    Guid AssetId,
    AssetIdentifierType IdentifierType,
    string IdentifierValue,
    bool IsPrimary = false);

public sealed record AssetScanResultDto(
    Guid AssetId,
    string AssetNumber,
    string Name,
    string? CategoryName,
    string? AssetTypeName,
    AssetStatus Status,
    AssetCondition Condition,
    AssetAssignmentType AssignmentType,
    string? CurrentBranchName,
    string? CurrentLocationName,
    string? CurrentCustodianEmployeeName,
    string? CurrentAssignedVehiclePlate,
    string PublicToken,
    bool RequiresInspectionSoon,
    bool RequiresCalibrationSoon);
