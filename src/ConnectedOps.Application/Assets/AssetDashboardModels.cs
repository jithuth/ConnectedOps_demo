namespace ConnectedOps.Application.Assets;

public sealed record AssetDashboardSummaryDto(
    int TotalAssets,
    int ActiveAssets,
    int InUseAssets,
    int AvailableAssets,
    int InMaintenanceAssets,
    int DisposedAssets,
    int TotalCategories,
    int TotalTypes,
    int OverdueInspectionsCount,
    int UpcomingInspectionsCount,
    int OverdueCalibrationsCount,
    int PendingTransfersCount,
    int IdleAssetsCount,
    decimal TotalAssetValue,
    List<CategoryBreakdownDto> CategoryBreakdown,
    List<StatusBreakdownDto> StatusBreakdown,
    List<ConditionBreakdownDto> ConditionBreakdown,
    List<UpcomingInspectionSummaryDto> UpcomingInspections,
    List<AssetTimelineEventDto> RecentActivities);

public sealed record CategoryBreakdownDto(
    Guid CategoryId,
    string CategoryName,
    int AssetCount,
    decimal TotalValue);

public sealed record StatusBreakdownDto(
    string Status,
    int Count,
    string ColorHex);

public sealed record ConditionBreakdownDto(
    string Condition,
    int Count,
    string ColorHex);

public sealed record UpcomingInspectionSummaryDto(
    Guid AssetId,
    string AssetNumber,
    string AssetName,
    DateTime DueDateUtc,
    bool IsOverdue,
    string InspectionType);
