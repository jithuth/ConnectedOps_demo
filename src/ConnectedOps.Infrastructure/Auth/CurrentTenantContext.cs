using ConnectedOps.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;
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
            }

            return null;
        }
    }

    public Guid? TenantUserId =>
        GetGuidClaim(
            ConnectedOpsClaimTypes.TenantUserId);

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