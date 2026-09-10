using System.IdentityModel.Tokens.Jwt;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformTokenServiceTests
{
    private readonly JwtTokenService _tokenService = TestJwtHelper.CreateTokenService();

    [Fact]
    public void CreatePlatformAccessToken_IncludesPlatformClaims_AndOmitsTenantClaims()
    {
        var user = new ApplicationUser
        {
            UserName = "admin@connectedops.com",
            Email = "admin@connectedops.com",
            PlatformRole = PlatformRole.SuperAdmin
        };

        var (tokenString, expiresAtUtc) = _tokenService.CreatePlatformAccessToken(user);

        Assert.NotNull(tokenString);
        Assert.True(expiresAtUtc > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        // Required platform claims
        Assert.Equal(user.Id.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.UserId)?.Value);
        Assert.Equal("admin@connectedops.com", jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value);
        Assert.Equal(PlatformRole.SuperAdmin.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.PlatformRole)?.Value);
        Assert.Equal("access", jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TokenType)?.Value);

        // MUST NOT contain tenant claims
        Assert.Null(jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantId));
        Assert.Null(jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantUserId));
    }

    [Fact]
    public void CreateAccessToken_ForTenant_IncludesTenantClaims()
    {
        var user = new ApplicationUser
        {
            UserName = "tenantuser@corp.com",
            Email = "tenantuser@corp.com",
            PlatformRole = PlatformRole.None
        };
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var (tokenString, expiresAtUtc) = _tokenService.CreateAccessToken(user, tenantId, tenantUserId);

        Assert.NotNull(tokenString);
        Assert.True(expiresAtUtc > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal(user.Id.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.UserId)?.Value);
        Assert.Equal(tenantId.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantId)?.Value);
        Assert.Equal(tenantUserId.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantUserId)?.Value);
        Assert.Equal("access", jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TokenType)?.Value);
        Assert.Equal(PlatformRole.None.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.PlatformRole)?.Value);
    }

    [Fact]
    public void CreateTenantSelectionToken_OmitsTenantId_AndSetsTokenTypeTenantSelection()
    {
        var user = new ApplicationUser
        {
            UserName = "user@corp.com",
            Email = "user@corp.com",
            PlatformRole = PlatformRole.None
        };

        var (tokenString, _) = _tokenService.CreateTenantSelectionToken(user);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal(user.Id.ToString(), jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.UserId)?.Value);
        Assert.Equal("tenant_selection", jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TokenType)?.Value);
        Assert.Null(jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantId));
        Assert.Null(jwt.Claims.FirstOrDefault(c => c.Type == ConnectedOpsClaimTypes.TenantUserId));
    }
}
