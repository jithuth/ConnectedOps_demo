namespace ConnectedOps.Application.Auth;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<LoginResult> SelectTenantAsync(
        SelectTenantRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResult> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        LogoutRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task LogoutAllAsync(
        Guid userId,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}