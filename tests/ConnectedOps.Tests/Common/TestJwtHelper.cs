using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Tests.Common;

public static class TestJwtHelper
{
    public static readonly JwtSettings DefaultSettings = new()
    {
        SigningKey = "SuperSecretKeyForTestingConnectedOpsSaaS12345678901234567890!",
        Issuer = "ConnectedOps.TestIssuer",
        Audience = "ConnectedOps.TestAudience",
        AccessTokenMinutes = 60,
        RefreshTokenDays = 30
    };

    public static JwtTokenService CreateTokenService(JwtSettings? settings = null)
    {
        return new JwtTokenService(Options.Create(settings ?? DefaultSettings));
    }
}
