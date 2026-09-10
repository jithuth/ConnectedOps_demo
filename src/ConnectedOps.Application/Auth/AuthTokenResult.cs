namespace ConnectedOps.Application.Auth;

public sealed record AuthTokenResult(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);