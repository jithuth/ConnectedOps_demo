using ConnectedOps.Application.Security;

namespace ConnectedOps.Application.Platform;

public sealed record PlatformSecurityLogQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? TenantId = null,
    Guid? UserId = null,
    string? Email = null,
    string? IpAddress = null,
    string? EventType = null,
    bool? Succeeded = null,
    int Page = 1,
    int PageSize = 20);

public sealed record PlatformSecurityDashboardStatsDto(
    int FailedLoginsToday,
    int LockedAccounts,
    int InvalidTokensToday,
    int SuspiciousEventsToday,
    int TotalEventsToday);

public interface IPlatformSecurityService
{
    Task<SecurityLogPage> GetSecurityLogsAsync(
        PlatformSecurityLogQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformSecurityDashboardStatsDto> GetSecurityDashboardStatsAsync(
        CancellationToken cancellationToken = default);
}
