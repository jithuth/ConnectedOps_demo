using System.Security.Cryptography;
using System.Text;

namespace ConnectedOps.Infrastructure.Auth;

public sealed class RefreshTokenService
    : IRefreshTokenService
{
    public string GenerateToken()
    {
        var bytes =
            RandomNumberGenerator.GetBytes(64);

        return Convert.ToBase64String(bytes);
    }

    public string HashToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException(
                "Refresh token is required.",
                nameof(token));
        }

        var bytes =
            Encoding.UTF8.GetBytes(token);

        var hash =
            SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }
}