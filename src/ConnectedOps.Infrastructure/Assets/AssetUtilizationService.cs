using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetUtilizationService : IAssetUtilizationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AssetUtilizationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<AssetUtilizationSummaryDto> GetUtilizationSummaryAsync(Guid? branchId = null, Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Include(x => x.AssetType)
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.UsageSessions)
            .Where(x => x.TenantId == tenantId && x.IsActive);

        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);
        if (categoryId.HasValue) query = query.Where(x => x.AssetCategoryId == categoryId.Value);

        var assets = await query.ToListAsync(cancellationToken);

        var totalCount = assets.Count;
        var inUseCount = assets.Count(x => x.Status == AssetStatus.InUse || x.Status == AssetStatus.CheckedOut);
        var availableCount = assets.Count(x => x.Status == AssetStatus.Available);
        var maintenanceCount = assets.Count(x => x.Status == AssetStatus.UnderMaintenance || x.Status == AssetStatus.UnderInspection);

        var now = DateTime.UtcNow;
        var idleThresholdDate = now.AddDays(-30);

        var idleList = new List<AssetIdleReportDto>();
        var topUsedList = new List<AssetUsageStatDto>();

        int neverUsedCount = 0;
        int idleCount = 0;

        foreach (var asset in assets)
        {
            var lastSession = asset.UsageSessions.OrderByDescending(s => s.CheckedOutAtUtc).FirstOrDefault();
            var totalSessions = asset.UsageSessions.Count;

            topUsedList.Add(new AssetUsageStatDto(
                asset.Id,
                asset.AssetNumber,
                asset.Name,
                asset.AssetCategory?.Name,
                0m,
                totalSessions,
                lastSession?.CheckedOutAtUtc));

            if (lastSession is null)
            {
                neverUsedCount++;
                var daysSinceCreation = (int)(now - asset.CreatedAtUtc).TotalDays;
                if (daysSinceCreation >= 14)
                {
                    idleList.Add(new AssetIdleReportDto(
                        asset.Id,
                        asset.AssetNumber,
                        asset.Name,
                        asset.AssetCategory?.Name,
                        asset.AssetType?.Name,
                        asset.Branch?.Name,
                        asset.Location?.Name,
                        null,
                        daysSinceCreation,
                        AssetUtilizationStatus.NeverUsed));
                }
            }
            else if (lastSession.CheckedOutAtUtc < idleThresholdDate && asset.Status == AssetStatus.Available)
            {
                idleCount++;
                var daysIdle = (int)(now - lastSession.CheckedOutAtUtc).TotalDays;
                idleList.Add(new AssetIdleReportDto(
                    asset.Id,
                    asset.AssetNumber,
                    asset.Name,
                    asset.AssetCategory?.Name,
                    asset.AssetType?.Name,
                    asset.Branch?.Name,
                    asset.Location?.Name,
                    lastSession.CheckedOutAtUtc,
                    daysIdle,
                    AssetUtilizationStatus.Idle));
            }
        }

        var utilizationRate = totalCount > 0
            ? Math.Round(((decimal)inUseCount / totalCount) * 100m, 1)
            : 0m;

        var orderedTopUsed = topUsedList
            .OrderByDescending(x => x.TotalSessionsCount)
            .Take(10)
            .ToList();

        var orderedIdle = idleList
            .OrderByDescending(x => x.IdleDays)
            .Take(15)
            .ToList();

        return new AssetUtilizationSummaryDto(
            totalCount,
            inUseCount,
            availableCount,
            maintenanceCount,
            idleCount,
            neverUsedCount,
            utilizationRate,
            orderedTopUsed,
            orderedIdle);
    }

    public async Task<IReadOnlyList<AssetIdleReportDto>> GetIdleAssetsAsync(int idleDaysThreshold = 30, Guid? branchId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.Assets
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Include(x => x.AssetType)
            .Include(x => x.Branch)
            .Include(x => x.Location)
            .Include(x => x.UsageSessions)
            .Where(x => x.TenantId == tenantId && x.IsActive && x.Status == AssetStatus.Available);

        if (branchId.HasValue) query = query.Where(x => x.BranchId == branchId.Value);

        var assets = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var thresholdDate = now.AddDays(-idleDaysThreshold);

        var idleList = new List<AssetIdleReportDto>();

        foreach (var asset in assets)
        {
            var lastSession = asset.UsageSessions.OrderByDescending(s => s.CheckedOutAtUtc).FirstOrDefault();
            if (lastSession is null)
            {
                var daysSinceCreation = (int)(now - asset.CreatedAtUtc).TotalDays;
                if (daysSinceCreation >= idleDaysThreshold)
                {
                    idleList.Add(new AssetIdleReportDto(
                        asset.Id,
                        asset.AssetNumber,
                        asset.Name,
                        asset.AssetCategory?.Name,
                        asset.AssetType?.Name,
                        asset.Branch?.Name,
                        asset.Location?.Name,
                        null,
                        daysSinceCreation,
                        AssetUtilizationStatus.NeverUsed));
                }
            }
            else if (lastSession.CheckedOutAtUtc < thresholdDate)
            {
                var daysIdle = (int)(now - lastSession.CheckedOutAtUtc).TotalDays;
                idleList.Add(new AssetIdleReportDto(
                    asset.Id,
                    asset.AssetNumber,
                    asset.Name,
                    asset.AssetCategory?.Name,
                    asset.AssetType?.Name,
                    asset.Branch?.Name,
                    asset.Location?.Name,
                    lastSession.CheckedOutAtUtc,
                    daysIdle,
                    AssetUtilizationStatus.Idle));
            }
        }

        return idleList.OrderByDescending(x => x.IdleDays).ToList();
    }
}
