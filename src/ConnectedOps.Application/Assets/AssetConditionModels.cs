using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetConditionRecordDto(
    Guid Id,
    Guid AssetId,
    string? AssetNumber,
    string? AssetName,
    AssetCondition Condition,
    DateTime RecordedAtUtc,
    string? RecordedByUserId,
    string? RecordedByUserName,
    string? Reason,
    string? Notes);

public sealed record CreateAssetConditionRecordRequest(
    Guid AssetId,
    AssetCondition Condition,
    string? Reason = null,
    string? Notes = null);
