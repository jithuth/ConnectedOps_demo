using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Application.Platform;

public sealed record PlatformDashboardStatsDto(
    int TotalTenants,
    int ActiveTenants,
    int PendingTenants,
    int SuspendedTenants,
    int DisabledTenants,
    int TotalUsers,
    int ActiveUsers,
    int PlatformUsers,
    int NewTenantsThisMonth,
    int NewUsersThisMonth,
    IReadOnlyCollection<RecentTenantDto> RecentTenants,
    IReadOnlyCollection<TenantGrowthPointDto> TenantGrowthTrend,
    IReadOnlyCollection<TenantStatusDistributionDto> StatusDistribution);

public sealed record RecentTenantDto(
    Guid Id,
    string Name,
    string Code,
    TenantStatus Status,
    DateTime CreatedAtUtc);

public sealed record TenantGrowthPointDto(
    string MonthLabel,
    int TenantCount);

public sealed record TenantStatusDistributionDto(
    string Status,
    int Count);

public interface IPlatformDashboardService
{
    Task<PlatformDashboardStatsDto> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default);
}
