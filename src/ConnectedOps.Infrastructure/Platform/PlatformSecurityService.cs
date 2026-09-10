using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Security;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformSecurityService : IPlatformSecurityService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformSecurityService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SecurityLogPage> GetSecurityLogsAsync(
        PlatformSecurityLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _dbContext.SecurityLogs
            .AsNoTracking()
            .AsQueryable();

        if (query.TenantId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.TenantId == query.TenantId.Value);
        }

        if (query.UserId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            var email = query.Email.Trim().ToLowerInvariant();
            dbQuery = dbQuery.Where(x => x.Email != null && x.Email.ToLower().Contains(email));
        }

        if (!string.IsNullOrWhiteSpace(query.IpAddress))
        {
            var ip = query.IpAddress.Trim();
            dbQuery = dbQuery.Where(x => x.IpAddress != null && x.IpAddress.Contains(ip));
        }

        if (!string.IsNullOrWhiteSpace(query.EventType))
        {
            dbQuery = dbQuery.Where(x => x.EventType.ToString() == query.EventType.Trim());
        }

        if (query.Succeeded.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.Succeeded == query.Succeeded.Value);
        }

        if (query.FromUtc.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc >= query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.CreatedAtUtc <= query.ToUtc.Value);
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var items = await dbQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SecurityLogDto(
                x.Id,
                x.TenantId,
                x.UserId,
                x.TenantUserId,
                x.EventType.ToString(),
                x.Succeeded,
                x.Email,
                x.Description,
                x.IpAddress,
                x.UserAgent,
                x.RequestPath,
                x.TraceId,
                x.Metadata,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new SecurityLogPage(items, page, pageSize, totalCount);
    }

    public async Task<PlatformSecurityDashboardStatsDto> GetSecurityDashboardStatsAsync(
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var failedLoginsToday = await _dbContext.SecurityLogs
            .CountAsync(x => x.CreatedAtUtc >= today &&
                             !x.Succeeded &&
                             (x.EventType == SecurityEventType.LoginFailed ||
                              x.EventType == SecurityEventType.TenantSelectionFailed),
                cancellationToken);

        var lockedAccounts = await _dbContext.Users
            .CountAsync(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow, cancellationToken);

        var invalidTokensToday = await _dbContext.SecurityLogs
            .CountAsync(x => x.CreatedAtUtc >= today &&
                             (x.EventType == SecurityEventType.InvalidAccessToken ||
                              x.EventType == SecurityEventType.ExpiredAccessToken),
                cancellationToken);

        var suspiciousEventsToday = await _dbContext.SecurityLogs
            .CountAsync(x => x.CreatedAtUtc >= today &&
                             (x.EventType == SecurityEventType.RefreshTokenReuseDetected ||
                              x.EventType == SecurityEventType.SuspiciousActivity),
                cancellationToken);

        var totalEventsToday = await _dbContext.SecurityLogs
            .CountAsync(x => x.CreatedAtUtc >= today, cancellationToken);

        return new PlatformSecurityDashboardStatsDto(
            failedLoginsToday,
            lockedAccounts,
            invalidTokensToday,
            suspiciousEventsToday,
            totalEventsToday);
    }
}
