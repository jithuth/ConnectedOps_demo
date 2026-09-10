using System.Text.Json;

using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Security;
using ConnectedOps.Infrastructure.Persistence;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Security;

public sealed class SecurityLogService
    : ISecurityLogService
{
    private static readonly JsonSerializerOptions
        JsonOptions = new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase
        };

    private readonly ConnectedOpsDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ConnectedOps.Application.Common.Interfaces.ICurrentTenantContext _tenantContext;

    public SecurityLogService(
        ConnectedOpsDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ConnectedOps.Application.Common.Interfaces.ICurrentTenantContext tenantContext)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _tenantContext = tenantContext;
    }

    public async Task WriteAsync(
        CreateSecurityLogRequest request,
        CancellationToken cancellationToken = default)
    {
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

        var securityLog =
            new SecurityLog(
                request.TenantId,
                request.UserId,
                request.TenantUserId,
                request.EventType,
                request.Succeeded,
                request.Email,
                request.Description,
                ipAddress,
                userAgent,
                requestPath,
                traceId,
                Serialize(request.Metadata));

        _dbContext.SecurityLogs.Add(
            securityLog);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<SecurityLogPage> GetAsync(
        SecurityLogQuery request,
        CancellationToken cancellationToken = default)
    {
        var page =
            Math.Max(
                request.Page,
                1);

        var pageSize =
            Math.Clamp(
                request.PageSize,
                1,
                100);

        var tenantId =
            _tenantContext.TenantId;

        if (!tenantId.HasValue ||
            tenantId.Value == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Current tenant is not available.");
        }

        var query =
            _dbContext.SecurityLogs
                .AsNoTracking()
                .Where(x =>
                    x.TenantId ==
                    tenantId.Value);

        if (request.UserId.HasValue)
        {
            query =
                query.Where(x =>
                    x.UserId ==
                    request.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                request.Email))
        {
            query =
                query.Where(x =>
                    x.Email ==
                    request.Email);
        }

        if (!string.IsNullOrWhiteSpace(
                request.EventType))
        {
            query =
                query.Where(x =>
                    x.EventType.ToString() ==
                    request.EventType);
        }

        if (request.Succeeded.HasValue)
        {
            query =
                query.Where(x =>
                    x.Succeeded ==
                    request.Succeeded.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                request.IpAddress))
        {
            query =
                query.Where(x =>
                    x.IpAddress ==
                    request.IpAddress);
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
                    new SecurityLogDto(
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
                .ToListAsync(
                    cancellationToken);

        return new SecurityLogPage(
            items,
            page,
            pageSize,
            totalCount);
    }

    private static string? Serialize(
        object? value)
    {
        return value is null
            ? null
            : JsonSerializer.Serialize(
                value,
                JsonOptions);
    }
}