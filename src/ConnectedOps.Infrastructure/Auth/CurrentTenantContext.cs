using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Security.Claims;

namespace ConnectedOps.Infrastructure.Auth;

public sealed class CurrentTenantContext
    : ICurrentTenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public Guid? UserId =>
        GetGuidClaim(
            ClaimTypes.NameIdentifier)
        ??
        GetGuidClaim("sub");

    public Guid? TenantId
    {
        get
        {
            var claim = GetGuidClaim(ConnectedOpsClaimTypes.TenantId);
            if (claim.HasValue)
                return claim;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null)
            {
                if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerVal) &&
                    Guid.TryParse(headerVal.FirstOrDefault(), out var headerGuid))
                {
                    return headerGuid;
                }

                if (httpContext.Request.Cookies.TryGetValue("ConnectedOps.ActiveTenantId", out var cookieVal) &&
                    Guid.TryParse(cookieVal, out var cookieGuid))
                {
                    return cookieGuid;
                }

                if (httpContext.Items.TryGetValue("ConnectedOps.ResolvedTenantId", out var itemVal) &&
                    itemVal is Guid itemGuid)
                {
                    return itemGuid;
                }

                // Dynamic fallback for authenticated sessions
                if (IsAuthenticated && UserId.HasValue)
                {
                    var db = httpContext.RequestServices.GetService<ConnectedOpsDbContext>();
                    if (db is not null)
                    {
                        var membershipTenantId = db.TenantUsers
                            .Where(tu => tu.UserId == UserId.Value && tu.IsActive && tu.Tenant.Status == Domain.Tenancy.TenantStatus.Active)
                            .OrderByDescending(tu => tu.IsDefaultTenant)
                            .Select(tu => (Guid?)tu.TenantId)
                            .FirstOrDefault();

                        if (membershipTenantId.HasValue)
                        {
                            httpContext.Items["ConnectedOps.ResolvedTenantId"] = membershipTenantId.Value;
                            return membershipTenantId.Value;
                        }

                        var platformRoleClaim = User?.FindFirst(ConnectedOpsClaimTypes.PlatformRole)?.Value;
                        if (!string.IsNullOrEmpty(platformRoleClaim) && platformRoleClaim != "None")
                        {
                            var activeTenantId = db.Tenants
                                .Where(t => t.Status == Domain.Tenancy.TenantStatus.Active)
                                .OrderBy(t => t.CreatedAtUtc)
                                .Select(t => (Guid?)t.Id)
                                .FirstOrDefault();

                            if (activeTenantId.HasValue)
                            {
                                httpContext.Items["ConnectedOps.ResolvedTenantId"] = activeTenantId.Value;
                                return activeTenantId.Value;
                            }
                        }
                    }
                }
            }

            return null;
        }
    }

    public Guid? TenantUserId
    {
        get
        {
            var claim = GetGuidClaim(ConnectedOpsClaimTypes.TenantUserId);
            if (claim.HasValue)
                return claim;

            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is not null && httpContext.Items.TryGetValue("ConnectedOps.ResolvedTenantUserId", out var itemVal) &&
                itemVal is Guid itemGuid)
            {
                return itemGuid;
            }

            if (IsAuthenticated && UserId.HasValue && TenantId.HasValue)
            {
                var db = httpContext?.RequestServices.GetService<ConnectedOpsDbContext>();
                if (db is not null)
                {
                    var tuId = db.TenantUsers
                        .Where(tu => tu.UserId == UserId.Value && tu.TenantId == TenantId.Value && tu.IsActive)
                        .Select(tu => (Guid?)tu.Id)
                        .FirstOrDefault();

                    if (tuId.HasValue && httpContext is not null)
                    {
                        httpContext.Items["ConnectedOps.ResolvedTenantUserId"] = tuId.Value;
                        return tuId.Value;
                    }
                }
            }

            return null;
        }
    }

    private Guid? GetGuidClaim(string claimType)
    {
        var value =
            User?.FindFirstValue(claimType);

        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Guid.TryParse(
            value,
            out var result)
                ? result
                : null;
    }
}