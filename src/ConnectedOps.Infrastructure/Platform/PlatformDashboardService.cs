using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformDashboardService : IPlatformDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformDashboardService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlatformDashboardStatsDto> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalTenants = await _dbContext.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _dbContext.Tenants.CountAsync(t => t.Status == TenantStatus.Active, cancellationToken);
        var pendingTenants = await _dbContext.Tenants.CountAsync(t => t.Status == TenantStatus.Pending, cancellationToken);
        var suspendedTenants = await _dbContext.Tenants.CountAsync(t => t.Status == TenantStatus.Suspended, cancellationToken);
        var disabledTenants = await _dbContext.Tenants.CountAsync(t => t.Status == TenantStatus.Disabled, cancellationToken);

        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await _dbContext.Users.CountAsync(u => u.IsActive, cancellationToken);
        var platformUsers = await _dbContext.Users.CountAsync(u => u.PlatformRole != PlatformRole.None, cancellationToken);

        var newTenantsThisMonth = await _dbContext.Tenants
            .CountAsync(t => t.CreatedAtUtc >= startOfMonth, cancellationToken);

        var newUsersThisMonth = await _dbContext.Users
            .CountAsync(u => u.CreatedAtUtc >= startOfMonth, cancellationToken);

        var recentTenants = await _dbContext.Tenants
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new RecentTenantDto(
                t.Id,
                t.Name,
                t.Code,
                t.Status,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        // Growth Trend (last 6 months)
        var growthTrend = new List<TenantGrowthPointDto>();
        for (var i = 5; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            var count = await _dbContext.Tenants
                .CountAsync(t => t.CreatedAtUtc < nextMonthStart, cancellationToken);

            var label = monthDate.ToString("MMM yyyy");
            growthTrend.Add(new TenantGrowthPointDto(label, count));
        }

        var statusDistribution = new List<TenantStatusDistributionDto>
        {
            new("Active", activeTenants),
            new("Pending", pendingTenants),
            new("Suspended", suspendedTenants),
            new("Disabled", disabledTenants)
        };

        return new PlatformDashboardStatsDto(
            totalTenants,
            activeTenants,
            pendingTenants,
            suspendedTenants,
            disabledTenants,
            totalUsers,
            activeUsers,
            platformUsers,
            newTenantsThisMonth,
            newUsersThisMonth,
            recentTenants,
            growthTrend,
            statusDistribution);
    }
}
