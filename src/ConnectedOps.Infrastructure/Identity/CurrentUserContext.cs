using System.Security.Claims;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ConnectedOps.Infrastructure.Identity;

public sealed class CurrentUserContext
    : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor
            .HttpContext?
            .User;

    public bool IsAuthenticated =>
        User?
            .Identity?
            .IsAuthenticated
        == true;

    public Guid? UserId =>
        GetGuidClaim(
            ConnectedOpsClaimTypes.UserId);

    public string? Email =>
        User?
            .FindFirstValue(
                ClaimTypes.Email);

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

                // Dynamic fallback for authenticated users
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

            if (httpContext is not null && IsAuthenticated && UserId.HasValue && TenantId.HasValue)
            {
                var db = httpContext.RequestServices.GetService<ConnectedOpsDbContext>();
                if (db is not null)
                {
                    var tuId = db.TenantUsers
                        .Where(tu => tu.UserId == UserId.Value && tu.TenantId == TenantId.Value && tu.IsActive)
                        .Select(tu => (Guid?)tu.Id)
                        .FirstOrDefault();

                    if (tuId.HasValue)
                    {
                        httpContext.Items["ConnectedOps.ResolvedTenantUserId"] = tuId.Value;
                        return tuId.Value;
                    }
                }
            }

            return null;
        }
    }

    private Guid? GetGuidClaim(
        string claimType)
    {
        var value =
            User?
                .FindFirstValue(
                    claimType);

        if (Guid.TryParse(
                value,
                out var result))
        {
            return result;
        }

        return null;
    }
}