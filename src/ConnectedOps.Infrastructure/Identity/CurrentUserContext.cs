using System.Security.Claims;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

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
            }

            return null;
        }
    }

    public Guid? TenantUserId =>
        GetGuidClaim(
            ConnectedOpsClaimTypes.TenantUserId);

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