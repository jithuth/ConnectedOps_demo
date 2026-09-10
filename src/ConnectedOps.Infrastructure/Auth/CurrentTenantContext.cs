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

    public Guid? TenantId =>
        GetGuidClaim(
            ConnectedOpsClaimTypes.TenantId);

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