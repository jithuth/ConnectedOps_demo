using ConnectedOps.Infrastructure.Identity;

namespace ConnectedOps.Infrastructure.Auth;

public interface IJwtTokenService
{
    /// <summary>
    /// Creates a tenant-scoped access token.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        ApplicationUser user,
        Guid tenantId,
        Guid tenantUserId);

    /// <summary>
    /// Creates a platform-scoped access token.
    ///
    /// Platform tokens do not contain TenantId or
    /// TenantUserId claims.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc)
        CreatePlatformAccessToken(
            ApplicationUser user);

    /// <summary>
    /// Creates a short-lived token used when the user
    /// belongs to multiple tenants.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc)
        CreateTenantSelectionToken(
            ApplicationUser user);

    /// <summary>
    /// Validates a tenant-selection token and returns
    /// the authenticated ApplicationUser Id.
    /// </summary>
    Guid ValidateTenantSelectionToken(
        string token);
}