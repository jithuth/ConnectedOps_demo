namespace ConnectedOps.Infrastructure.Auth;

public interface IRefreshTokenService
{
    string GenerateToken();

    string HashToken(string token);
}