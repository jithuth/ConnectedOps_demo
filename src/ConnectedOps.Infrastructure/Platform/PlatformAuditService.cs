using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformAuditService : IPlatformAuditService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public PlatformAuditService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AuditLogPage> GetAuditLogsAsync(
        PlatformAuditLogQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var dbQuery = _dbContext.AuditLogs
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

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            dbQuery = dbQuery.Where(x => x.Action.ToString() == query.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            dbQuery = dbQuery.Where(x => x.EntityType == query.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.EntityId))
        {
            dbQuery = dbQuery.Where(x => x.EntityId == query.EntityId.Trim());
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
            .Select(x => new AuditLogDto(
                x.Id,
                x.TenantId,
                x.UserId,
                x.TenantUserId,
                x.Action.ToString(),
                x.EntityType,
                x.EntityId,
                x.Description,
                x.OldValues,
                x.NewValues,
                x.IpAddress,
                x.UserAgent,
                x.RequestPath,
                x.TraceId,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new AuditLogPage(items, page, pageSize, totalCount);
    }
}
