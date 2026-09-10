using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Persistence;
using ConnectedOps.Infrastructure.Platform;
using ConnectedOps.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformBootstrapServiceTests
{
    private (IServiceProvider Provider, ConnectedOpsDbContext Context) CreateServiceProvider(PlatformBootstrapOptions options)
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddDbContext<ConnectedOpsDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>(opt =>
        {
            opt.Password.RequireDigit = true;
            opt.Password.RequiredLength = 8;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
            opt.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<ConnectedOpsDbContext>();

        services.AddLogging(builder => builder.AddConsole());
        services.AddSingleton(Options.Create(options));

        var provider = services.BuildServiceProvider();

        // Ensure DB is created
        var context = provider.GetRequiredService<ConnectedOpsDbContext>();
        context.Database.EnsureCreated();

        return (provider, context);
    }

    [Fact]
    public async Task BootstrapPlatformAsync_CreatesSuperAdmin_WhenUserDoesNotExist()
    {
        var options = new PlatformBootstrapOptions
        {
            Enabled = true,
            Email = "superadmin@connectedops.com",
            Password = "SuperAdminPassword123!",
            FirstName = "Platform",
            LastName = "Admin"
        };

        var (provider, context) = CreateServiceProvider(options);

        await provider.BootstrapPlatformAsync();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("superadmin@connectedops.com");

        Assert.NotNull(user);
        Assert.Equal(PlatformRole.SuperAdmin, user.PlatformRole);
        Assert.Equal("Platform", user.FirstName);
        Assert.Equal("Admin", user.LastName);
        Assert.True(user.IsActive);
        Assert.True(await userManager.CheckPasswordAsync(user, "SuperAdminPassword123!"));
    }

    [Fact]
    public async Task BootstrapPlatformAsync_IsIdempotent_WhenSuperAdminAlreadyExists()
    {
        var options = new PlatformBootstrapOptions
        {
            Enabled = true,
            Email = "superadmin@connectedops.com",
            Password = "SuperAdminPassword123!",
            FirstName = "Platform",
            LastName = "Admin"
        };

        var (provider, context) = CreateServiceProvider(options);

        // Run first time
        await provider.BootstrapPlatformAsync();

        // Run second time
        await provider.BootstrapPlatformAsync();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var count = await userManager.Users.CountAsync(u => u.Email == "superadmin@connectedops.com");

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task BootstrapPlatformAsync_ThrowsException_WhenEmailBelongsToNonSuperAdmin()
    {
        var options = new PlatformBootstrapOptions
        {
            Enabled = true,
            Email = "tenantuser@company.com",
            Password = "SuperAdminPassword123!",
            FirstName = "Hacker",
            LastName = "Admin"
        };

        var (provider, context) = CreateServiceProvider(options);
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        // Pre-create a regular user (PlatformRole == None)
        var normalUser = new ApplicationUser
        {
            UserName = "tenantuser@company.com",
            Email = "tenantuser@company.com",
            FirstName = "John",
            LastName = "Tenant",
            PlatformRole = PlatformRole.None,
            IsActive = true
        };
        await userManager.CreateAsync(normalUser, "TenantUserPassword123!");

        // Attempting to bootstrap with that email must fail and NOT silently promote
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.BootstrapPlatformAsync());

        Assert.Contains("Promotion is rejected", ex.Message);

        var refreshedUser = await userManager.FindByEmailAsync("tenantuser@company.com");
        Assert.NotNull(refreshedUser);
        Assert.Equal(PlatformRole.None, refreshedUser.PlatformRole); // Still None
    }

    [Fact]
    public async Task BootstrapPlatformAsync_DoesNothing_WhenDisabled()
    {
        var options = new PlatformBootstrapOptions
        {
            Enabled = false,
            Email = "superadmin@connectedops.com",
            Password = "SuperAdminPassword123!"
        };

        var (provider, context) = CreateServiceProvider(options);

        await provider.BootstrapPlatformAsync();

        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync("superadmin@connectedops.com");

        Assert.Null(user);
    }
}
