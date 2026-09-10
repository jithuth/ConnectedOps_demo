using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Platform;
using ConnectedOps.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformUserServiceTests
{
    [Fact]
    public async Task GetUsersAsync_ReturnsPlatformUsers_WithFiltering()
    {
        using var context = TestDbContextFactory.Create();
        var userManager = TestIdentityHelper.CreateUserManager(context);
        var service = new PlatformUserService(userManager, context);

        var superAdmin = new ApplicationUser
        {
            UserName = "admin@platform.com",
            Email = "admin@platform.com",
            FirstName = "Super",
            LastName = "Admin",
            PlatformRole = PlatformRole.SuperAdmin,
            IsActive = true
        };

        var supportUser = new ApplicationUser
        {
            UserName = "support@platform.com",
            Email = "support@platform.com",
            FirstName = "Help",
            LastName = "Desk",
            PlatformRole = PlatformRole.SupportAdmin,
            IsActive = false
        };

        var tenantUser = new ApplicationUser
        {
            UserName = "tenant@client.com",
            Email = "tenant@client.com",
            PlatformRole = PlatformRole.None,
            IsActive = true
        };

        await userManager.CreateAsync(superAdmin, "AdminPassword123!");
        await userManager.CreateAsync(supportUser, "SupportPassword123!");
        await userManager.CreateAsync(tenantUser, "TenantPassword123!");

        // 1. Query all platform users (tenant user should be excluded by default)
        var allPlatform = await service.GetUsersAsync(new PlatformUserQuery());
        Assert.Equal(2, allPlatform.TotalCount);
        Assert.DoesNotContain(allPlatform.Items, u => u.Email == "tenant@client.com");

        // 2. Query by role
        var supportOnly = await service.GetUsersAsync(new PlatformUserQuery(Role: PlatformRole.SupportAdmin));
        Assert.Equal(1, supportOnly.TotalCount);
        Assert.Equal("support@platform.com", supportOnly.Items.First().Email);

        // 3. Query active only
        var activeOnly = await service.GetUsersAsync(new PlatformUserQuery(IsActive: true));
        Assert.Equal(1, activeOnly.TotalCount);
        Assert.Equal("admin@platform.com", activeOnly.Items.First().Email);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_And_DeactivateUserAsync_ModifyUserState()
    {
        using var context = TestDbContextFactory.Create();
        var userManager = TestIdentityHelper.CreateUserManager(context);
        var service = new PlatformUserService(userManager, context);

        var user = new ApplicationUser
        {
            UserName = "mod@platform.com",
            Email = "mod@platform.com",
            PlatformRole = PlatformRole.SupportAdmin,
            IsActive = true
        };
        await userManager.CreateAsync(user, "Password123!");

        // Update role to BillingAdmin
        await service.UpdateUserRoleAsync(user.Id, PlatformRole.BillingAdmin);
        var refreshedRole = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.Equal(PlatformRole.BillingAdmin, refreshedRole!.PlatformRole);

        // Deactivate user
        await service.DeactivateUserAsync(user.Id);
        var refreshed = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.False(refreshed!.IsActive);

        // Activate user
        await service.ActivateUserAsync(user.Id);
        var activated = await userManager.FindByIdAsync(user.Id.ToString());
        Assert.True(activated!.IsActive);
    }

    [Fact]
    public async Task CreateUserAsync_CreatesAdminWithPlatformRole()
    {
        using var context = TestDbContextFactory.Create();
        var userManager = TestIdentityHelper.CreateUserManager(context);
        var service = new PlatformUserService(userManager, context);

        var request = new CreatePlatformUserRequest(
            Email: "newadmin@platform.com",
            FirstName: "New",
            LastName: "Admin",
            Role: PlatformRole.PlatformAdmin);

        var created = await service.CreateUserAsync(request);

        Assert.Equal("newadmin@platform.com", created.User.Email);
        Assert.Equal(PlatformRole.PlatformAdmin, created.User.PlatformRole);
        Assert.True(created.User.IsActive);
    }
}
