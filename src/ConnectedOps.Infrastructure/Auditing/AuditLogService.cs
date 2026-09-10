using System.Text.Json;

using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Auditing;

public sealed class AuditLogService
    : IAuditLogService
{
    private static readonly JsonSerializerOptions
        JsonOptions = new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentTenantContext _tenantContext;
    private readonly ICurrentUserContext _userContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        ConnectedOpsDbContext dbContext,
        ICurrentTenantContext tenantContext,
        ICurrentUserContext userContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _tenantContext = tenantContext;
        _userContext = userContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(
        CreateAuditLogRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        if (!tenantId.HasValue ||
            tenantId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Current tenant is not available.");
        }

        var httpContext =
            _httpContextAccessor.HttpContext;

        var ipAddress =
            httpContext?
                .Connection
                .RemoteIpAddress?
                .ToString();

        var userAgent =
            httpContext?
                .Request
                .Headers
                .UserAgent
                .ToString();

        var requestPath =
            httpContext?
                .Request
                .Path
                .ToString();

        var traceId =
            httpContext?
                .TraceIdentifier;

        var auditLog =
            new AuditLog(
                tenantId.Value,
                _userContext.UserId,
                _userContext.TenantUserId,
                request.Action,
                request.EntityType,
                request.EntityId,
                request.Description,
                Serialize(request.OldValues),
                Serialize(request.NewValues),
                ipAddress,
                userAgent,
                requestPath,
                traceId);

        _dbContext.AuditLogs.Add(
            auditLog);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<AuditLogPage> GetAsync(
        AuditLogQuery request,
        CancellationToken cancellationToken = default)
    {
        var tenantId =
            _tenantContext.TenantId;

        if (!tenantId.HasValue ||
            tenantId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Current tenant is not available.");
        }

        var page =
            Math.Max(request.Page, 1);

        var pageSize =
            Math.Clamp(
                request.PageSize,
                1,
                100);

        var query =
            _dbContext.AuditLogs
                .AsNoTracking()
                .Where(x =>
                    x.TenantId ==
                    tenantId.Value);

        if (!string.IsNullOrWhiteSpace(
                request.EntityType))
        {
            query =
                query.Where(x =>
                    x.EntityType ==
                    request.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(
                request.EntityId))
        {
            query =
                query.Where(x =>
                    x.EntityId ==
                    request.EntityId);
        }

        if (request.UserId.HasValue)
        {
            query =
                query.Where(x =>
                    x.UserId ==
                    request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                request.Action))
        {
            query =
                query.Where(x =>
                    x.Action.ToString() ==
                    request.Action);
        }

        if (request.FromUtc.HasValue)
        {
            query =
                query.Where(x =>
                    x.CreatedAtUtc >=
                    request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query =
                query.Where(x =>
                    x.CreatedAtUtc <=
                    request.ToUtc.Value);
        }

        var totalCount =
            await query.CountAsync(
                cancellationToken);

        var items =
            await query
                .OrderByDescending(x =>
                    x.CreatedAtUtc)
                .Skip(
                    (page - 1) *
                    pageSize)
                .Take(pageSize)
                .Select(x =>
                    new AuditLogDto(
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
                .ToListAsync(
                    cancellationToken);

        return new AuditLogPage(
            items,
            page,
            pageSize,
            totalCount);
    }

    private static string? Serialize(
        object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            value,
            JsonOptions);
    }
}