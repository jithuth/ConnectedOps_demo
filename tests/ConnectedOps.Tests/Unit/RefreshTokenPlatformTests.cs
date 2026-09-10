using ConnectedOps.Domain.Auth;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class RefreshTokenPlatformTests
{
    [Fact]
    public void RefreshToken_CanBeCreatedForPlatformSession_WithoutTenant()
    {
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddDays(7);

        var token = new RefreshToken(
            userId: userId,
            tenantId: null,
            tenantUserId: null,
            tokenHash: "hashed_refresh_token_123",
            familyId: familyId,
            expiresAtUtc: expiresAtUtc,
            createdByIp: "127.0.0.1");

        Assert.Equal(userId, token.UserId);
        Assert.Null(token.TenantId);
        Assert.Null(token.TenantUserId);
        Assert.Equal("hashed_refresh_token_123", token.TokenHash);
        Assert.Equal(familyId, token.FamilyId);
        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsUsed);
    }

    [Fact]
    public void RefreshToken_CanBeCreatedForTenantSession_WithTenantAndTenantUserId()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var expiresAtUtc = DateTime.UtcNow.AddDays(7);

        var token = new RefreshToken(
            userId: userId,
            tenantId: tenantId,
            tenantUserId: tenantUserId,
            tokenHash: "hashed_refresh_token_456",
            familyId: familyId,
            expiresAtUtc: expiresAtUtc,
            createdByIp: "127.0.0.1");

        Assert.Equal(userId, token.UserId);
        Assert.Equal(tenantId, token.TenantId);
        Assert.Equal(tenantUserId, token.TenantUserId);
        Assert.True(token.IsActive);
    }

    [Fact]
    public void RefreshToken_ThrowsWhenTenantIdProvidedWithoutTenantUserId()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(
            userId: userId,
            tenantId: tenantId,
            tenantUserId: null,
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        Assert.Contains("must both be provided", ex.Message);
    }

    [Fact]
    public void RefreshToken_ThrowsWhenTenantUserIdProvidedWithoutTenantId()
    {
        var userId = Guid.NewGuid();
        var tenantUserId = Guid.NewGuid();

        var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(
            userId: userId,
            tenantId: null,
            tenantUserId: tenantUserId,
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        Assert.Contains("must both be provided", ex.Message);
    }

    [Fact]
    public void RefreshToken_ThrowsWhenTenantIdIsEmptyGuid()
    {
        var ex = Assert.Throws<ArgumentException>(() => new RefreshToken(
            userId: Guid.NewGuid(),
            tenantId: Guid.Empty,
            tenantUserId: Guid.NewGuid(),
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            expiresAtUtc: DateTime.UtcNow.AddDays(1)));

        Assert.Contains("TenantId cannot be empty", ex.Message);
    }

    [Fact]
    public void RefreshToken_RevokeAndMarkUsed_TransitionsStateCorrectly()
    {
        var token = new RefreshToken(
            userId: Guid.NewGuid(),
            tenantId: null,
            tenantUserId: null,
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            expiresAtUtc: DateTime.UtcNow.AddDays(1));

        Assert.True(token.IsActive);

        token.MarkUsed();
        Assert.True(token.IsUsed);
        Assert.False(token.IsActive);

        token.Revoke("10.0.0.1");
        Assert.True(token.IsRevoked);
        Assert.Equal("10.0.0.1", token.RevokedByIp);
    }
}
