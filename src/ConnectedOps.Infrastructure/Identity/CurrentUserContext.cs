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

    public Guid? TenantId =>
        GetGuidClaim(
            ConnectedOpsClaimTypes.TenantId);

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