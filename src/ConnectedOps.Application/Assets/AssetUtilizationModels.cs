using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetUtilizationSummaryDto(
    int TotalAssets,
    int InUseCount,
    int AvailableCount,
    int MaintenanceCount,
    int IdleAssetsCount,
    int NeverUsedAssetsCount,
    decimal OverallUtilizationRatePercentage,
    List<AssetUsageStatDto> TopUsedAssets,
    List<AssetIdleReportDto> IdleAssets);

public sealed record AssetIdleReportDto(
    Guid AssetId,
    string AssetNumber,
    string Name,
    string? CategoryName,
    string? AssetTypeName,
    string? CurrentBranchName,
    string? CurrentLocationName,
    DateTime? LastUsedAtUtc,
    int IdleDays,
    AssetUtilizationStatus Status);

public sealed record AssetUsageStatDto(
    Guid AssetId,
    string AssetNumber,
    string Name,
    string? CategoryName,
    decimal TotalUsageHours,
    int TotalSessionsCount,
    DateTime? LastUsedAtUtc);
