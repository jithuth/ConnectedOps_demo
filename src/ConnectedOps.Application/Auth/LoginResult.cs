namespace ConnectedOps.Application.Auth;

public sealed record LoginResult(
    bool RequiresTenantSelection,
    string? AccessToken,
    DateTime? ExpiresAtUtc,
    string? RefreshToken,
    DateTime? RefreshTokenExpiresAtUtc,
    string? TenantSelectionToken,
    DateTime? TenantSelectionTokenExpiresAtUtc,
    IReadOnlyCollection<LoginTenantResult> Tenants);

public sealed record LoginTenantResult(
    Guid TenantId,
    Guid TenantUserId,
    string TenantName,
    string TenantCode);