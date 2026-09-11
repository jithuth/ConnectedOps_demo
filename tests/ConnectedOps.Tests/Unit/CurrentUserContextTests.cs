using System.Security.Claims;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Auth;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class CurrentUserContextTests
{
    private (ConnectedOpsDbContext Context, IServiceProvider ServiceProvider) CreateDbContext()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ConnectedOpsDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var ctx = sp.GetRequiredService<ConnectedOpsDbContext>();
        ctx.Database.EnsureCreated();

        return (ctx, sp);
    }

    [Fact]
    public void TenantId_ResolvesFromClaim_WhenPresent()
    {
        var expectedTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ConnectedOpsClaimTypes.TenantId, expectedTenantId.ToString()),
            new(ConnectedOpsClaimTypes.UserId, Guid.NewGuid().ToString())
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserContext(accessor);

        Assert.Equal(expectedTenantId, context.TenantId);
    }

    [Fact]
    public void TenantId_ResolvesFromCookie_WhenClaimMissing()
    {
        var expectedTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Cookie"] = $"ConnectedOps.ActiveTenantId={expectedTenantId}";
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ConnectedOpsClaimTypes.UserId, Guid.NewGuid().ToString())
        ], "Test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserContext(accessor);

        Assert.Equal(expectedTenantId, context.TenantId);
    }

    [Fact]
    public void TenantId_ResolvesFromDatabase_ForSuperAdmin_WhenClaimAndCookieMissing()
    {
        var (dbContext, sp) = CreateDbContext();

        var tenant = new Tenant("Test Tenant", "TEST-CODE", "test@company.com");
        dbContext.Tenants.Add(tenant);
        dbContext.SaveChanges();

        var userId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = sp
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ConnectedOpsClaimTypes.UserId, userId.ToString()),
            new Claim(ConnectedOpsClaimTypes.PlatformRole, PlatformRole.SuperAdmin.ToString())
        ], "Test"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var context = new CurrentUserContext(accessor);

        Assert.Equal(tenant.Id, context.TenantId);
    }
}
