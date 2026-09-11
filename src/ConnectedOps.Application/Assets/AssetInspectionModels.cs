using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetInspectionDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    AssetInspectionType InspectionType,
    DateTime InspectionDateUtc,
    Guid? InspectorEmployeeId,
    string? InspectorEmployeeName,
    string? InspectorUserId,
    string? InspectorUserName,
    AssetInspectionStatus Status,
    AssetInspectionResult Result,
    string? Remarks,
    DateTime? NextInspectionDueUtc,
    DateTime CreatedAtUtc,
    List<AssetInspectionItemDto> Items);

public sealed record AssetInspectionItemDto(
    Guid Id,
    Guid InspectionId,
    string CheckItemName,
    bool IsPassed,
    string? Comments,
    string? Severity);

public sealed record CreateAssetInspectionRequest(
    Guid AssetId,
    AssetInspectionType InspectionType,
    DateTime InspectionDateUtc,
    Guid? InspectorEmployeeId,
    AssetInspectionResult Result,
    string? Remarks,
    DateTime? NextInspectionDueUtc,
    List<CreateAssetInspectionItemRequest>? Items = null);

public sealed record CreateAssetInspectionItemRequest(
    string CheckItemName,
    bool IsPassed,
    string? Comments = null,
    string? Severity = null);

public sealed record CompleteAssetInspectionRequest(
    AssetInspectionResult Result,
    string? Remarks,
    DateTime? NextInspectionDueUtc);

public sealed record AssetCalibrationRecordDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    DateTime CalibrationDateUtc,
    string PerformedBy,
    string? CertificateNumber,
    DateTime NextCalibrationDueUtc,
    bool IsPassed,
    string? Remarks,
    DateTime CreatedAtUtc);

public sealed record CreateAssetCalibrationRecordRequest(
    Guid AssetId,
    DateTime CalibrationDateUtc,
    string PerformedBy,
    string? CertificateNumber,
    DateTime NextCalibrationDueUtc,
    bool IsPassed,
    string? Remarks = null);

public sealed record AssetInspectionFilter(
    Guid? AssetId = null,
    AssetInspectionType? InspectionType = null,
    AssetInspectionStatus? Status = null,
    AssetInspectionResult? Result = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int PageNumber = 1,
    int PageSize = 20);
