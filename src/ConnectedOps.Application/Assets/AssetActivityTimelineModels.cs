namespace ConnectedOps.Application.Assets;

public sealed record AssetTimelineEventDto(
    Guid Id,
    Guid AssetId,
    string EventType,
    string Title,
    string? Description,
    DateTime TimestampUtc,
    string? PerformedByUserId,
    string? PerformedByUserName,
    string? BadgeClass,
    string? Icon);

public sealed record AssetActivityTimelineDto(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    List<AssetTimelineEventDto> Events);
