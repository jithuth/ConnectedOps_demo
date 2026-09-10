using System.Security.Claims;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformAuthorizationHandlerTests
{
    private readonly PlatformRoleAuthorizationHandler _handler = new();

    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal principal, PlatformRole requiredRole)
    {
        var requirement = new PlatformRoleRequirement(requiredRole);
        return new AuthorizationHandlerContext(new[] { requirement }, principal, null);
    }

    [Fact]
    public async Task HandleAsync_Fails_WhenUserIsNotAuthenticated()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated
        var context = CreateContext(principal, PlatformRole.SuperAdmin);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Fails_WhenPlatformRoleClaimIsMissing()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.UserId, Guid.NewGuid().ToString()));
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.TenantId, Guid.NewGuid().ToString()));
        var principal = new ClaimsPrincipal(identity);

        var context = CreateContext(principal, PlatformRole.SuperAdmin);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Fails_WhenPlatformRoleIsNone()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.PlatformRole, PlatformRole.None.ToString()));
        var principal = new ClaimsPrincipal(identity);

        var context = CreateContext(principal, PlatformRole.SuperAdmin);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserIsSuperAdmin()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.PlatformRole, PlatformRole.SuperAdmin.ToString()));
        var principal = new ClaimsPrincipal(identity);

        // SuperAdmin satisfies SuperAdmin requirement
        var context1 = CreateContext(principal, PlatformRole.SuperAdmin);
        await _handler.HandleAsync(context1);
        Assert.True(context1.HasSucceeded);

        // SuperAdmin also satisfies PlatformAdmin requirement
        var context2 = CreateContext(principal, PlatformRole.PlatformAdmin);
        await _handler.HandleAsync(context2);
        Assert.True(context2.HasSucceeded);

        // SuperAdmin also satisfies SupportAdmin requirement
        var context3 = CreateContext(principal, PlatformRole.SupportAdmin);
        await _handler.HandleAsync(context3);
        Assert.True(context3.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Succeeds_WhenUserMatchesExactRole()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.PlatformRole, PlatformRole.SupportAdmin.ToString()));
        var principal = new ClaimsPrincipal(identity);

        var context = CreateContext(principal, PlatformRole.SupportAdmin);
        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_Fails_WhenNonSuperAdminAttemptsHigherOrDifferentRole()
    {
        var identity = new ClaimsIdentity("TestAuth");
        identity.AddClaim(new Claim(ConnectedOpsClaimTypes.PlatformRole, PlatformRole.SupportAdmin.ToString()));
        var principal = new ClaimsPrincipal(identity);

        // SupportAdmin cannot satisfy SuperAdmin
        var context1 = CreateContext(principal, PlatformRole.SuperAdmin);
        await _handler.HandleAsync(context1);
        Assert.False(context1.HasSucceeded);

        // SupportAdmin cannot satisfy BillingAdmin
        var context2 = CreateContext(principal, PlatformRole.BillingAdmin);
        await _handler.HandleAsync(context2);
        Assert.False(context2.HasSucceeded);
    }
}
