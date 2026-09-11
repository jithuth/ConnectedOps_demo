using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetDashboardService : IAssetDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AssetDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<AssetDashboardSummaryDto> GetDashboardSummaryAsync(Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var assetQuery = _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Include(x => x.AssetType)
            .Include(x => x.Inspections)
            .Include(x => x.CalibrationRecords)
            .Where(x => x.TenantId == tenantId);

        if (branchId.HasValue)
        {
            assetQuery = assetQuery.Where(x => x.BranchId == branchId.Value);
        }

        var assets = await assetQuery.ToListAsync(cancellationToken);

        var totalAssets = assets.Count;
        var activeAssets = assets.Count(x => x.IsActive);
        var inUseAssets = assets.Count(x => x.Status == AssetStatus.InUse || x.Status == AssetStatus.CheckedOut);
        var availableAssets = assets.Count(x => x.Status == AssetStatus.Available);
        var inMaintenanceAssets = assets.Count(x => x.Status == AssetStatus.UnderMaintenance || x.Status == AssetStatus.UnderInspection);
        var disposedAssets = assets.Count(x => x.Status == AssetStatus.Disposed || x.Status == AssetStatus.Retired);

        var totalValue = assets.Sum(x => x.PurchaseCost ?? 0m);

        var totalCategories = await _dbContext.AssetCategories
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);
        var totalTypes = await _dbContext.AssetTypes
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var now = DateTime.UtcNow;
        var upcomingWindow = now.AddDays(30);

        var allInspections = assets.SelectMany(x => x.Inspections).ToList();
        var allCalibrations = assets.SelectMany(x => x.CalibrationRecords).ToList();

        var overdueInspectionsCount = allInspections.Count(x => x.NextInspectionDateUtc.HasValue && x.NextInspectionDateUtc.Value < now);
        var upcomingInspectionsCount = allInspections.Count(x => x.NextInspectionDateUtc.HasValue && x.NextInspectionDateUtc.Value >= now && x.NextInspectionDateUtc.Value <= upcomingWindow);
        var overdueCalibrationsCount = allCalibrations.Count(x => x.NextCalibrationDateUtc.HasValue && x.NextCalibrationDateUtc.Value < now);

        var pendingTransfersCount = await _dbContext.AssetTransfers
            .CountAsync(x => x.TenantId == tenantId && (x.Status == AssetTransferStatus.Pending || x.Status == AssetTransferStatus.InTransit), cancellationToken);

        var idleAssetsCount = assets.Count(x => x.Status == AssetStatus.Available && x.CreatedAtUtc < now.AddDays(-30));

        // Category breakdown
        var categoryBreakdown = assets
            .GroupBy(x => new { x.AssetCategoryId, Name = x.AssetCategory?.Name ?? "Uncategorized" })
            .Select(g => new CategoryBreakdownDto(
                g.Key.AssetCategoryId,
                g.Key.Name,
                g.Count(),
                g.Sum(x => x.PurchaseCost ?? 0m)))
            .OrderByDescending(x => x.AssetCount)
            .ToList();

        // Status breakdown
        var statusColors = new Dictionary<AssetStatus, string>
        {
            [AssetStatus.Draft] = "#6c757d",
            [AssetStatus.Available] = "#28a745",
            [AssetStatus.Assigned] = "#17a2b8",
            [AssetStatus.CheckedOut] = "#ffc107",
            [AssetStatus.InUse] = "#fd7e14",
            [AssetStatus.UnderInspection] = "#6f42c1",
            [AssetStatus.UnderMaintenance] = "#e83e8c",
            [AssetStatus.Damaged] = "#dc3545",
            [AssetStatus.Lost] = "#343a40",
            [AssetStatus.Inactive] = "#adb5bd",
            [AssetStatus.Retired] = "#6c757d",
            [AssetStatus.Disposed] = "#495057"
        };

        var statusBreakdown = assets
            .GroupBy(x => x.Status)
            .Select(g => new StatusBreakdownDto(
                g.Key.ToString(),
                g.Count(),
                statusColors.GetValueOrDefault(g.Key, "#6c757d")))
            .OrderByDescending(x => x.Count)
            .ToList();

        // Condition breakdown
        var conditionColors = new Dictionary<AssetCondition, string>
        {
            [AssetCondition.Excellent] = "#28a745",
            [AssetCondition.Good] = "#20c997",
            [AssetCondition.Fair] = "#ffc107",
            [AssetCondition.NeedsAttention] = "#fd7e14",
            [AssetCondition.Damaged] = "#dc3545",
            [AssetCondition.Unsafe] = "#721c24"
        };

        var conditionBreakdown = assets
            .GroupBy(x => x.Condition)
            .Select(g => new ConditionBreakdownDto(
                g.Key.ToString(),
                g.Count(),
                conditionColors.GetValueOrDefault(g.Key, "#6c757d")))
            .OrderByDescending(x => x.Count)
            .ToList();

        // Upcoming / overdue inspections
        var upcomingInspections = allInspections
            .Where(x => x.NextInspectionDateUtc.HasValue && x.NextInspectionDateUtc.Value <= upcomingWindow)
            .OrderBy(x => x.NextInspectionDateUtc)
            .Take(5)
            .Select(x => new UpcomingInspectionSummaryDto(
                x.AssetId,
                x.Asset?.AssetNumber ?? "",
                x.Asset?.Name ?? "",
                x.NextInspectionDateUtc!.Value,
                x.NextInspectionDateUtc.Value < now,
                x.InspectionType.ToString()))
            .ToList();

        // Recent activities
        var recentAuditLogs = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && (x.EntityType == nameof(Asset) || x.EntityType == nameof(AssetTransfer) || x.EntityType == nameof(AssetInspection) || x.EntityType == nameof(AssetUsageSession) || x.EntityType == nameof(AssetEmployeeAssignment)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentActivities = recentAuditLogs.Select(al => new AssetTimelineEventDto(
            al.Id,
            Guid.TryParse(al.EntityId, out var g) ? g : Guid.Empty,
            al.Action.ToString(),
            al.Action.ToString(),
            al.Description ?? al.Action.ToString(),
            al.CreatedAtUtc,
            al.UserId?.ToString(),
            null,
            "badge-primary",
            "fas fa-info-circle")).ToList();

        return new AssetDashboardSummaryDto(
            totalAssets,
            activeAssets,
            inUseAssets,
            availableAssets,
            inMaintenanceAssets,
            disposedAssets,
            totalCategories,
            totalTypes,
            overdueInspectionsCount,
            upcomingInspectionsCount,
            overdueCalibrationsCount,
            pendingTransfersCount,
            idleAssetsCount,
            totalValue,
            categoryBreakdown,
            statusBreakdown,
            conditionBreakdown,
            upcomingInspections,
            recentActivities);
    }
}
