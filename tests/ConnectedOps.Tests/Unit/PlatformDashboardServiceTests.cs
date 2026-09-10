using ConnectedOps.Domain.Platform;
using ConnectedOps.Domain.Tenancy;
using ConnectedOps.Infrastructure.Identity;
using ConnectedOps.Infrastructure.Platform;
using ConnectedOps.Tests.Common;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformDashboardServiceTests
{
    [Fact]
    public async Task GetDashboardStatsAsync_CalculatesAccurateRealStatistics()
    {
        using var context = TestDbContextFactory.Create();
        var service = new PlatformDashboardService(context);

        // Add tenants
        var activeTenant1 = new Tenant("Alpha", "ALPHA");
        var activeTenant2 = new Tenant("Beta", "BETA");
        var suspendedTenant = new Tenant("Gamma", "GAMMA");
        suspendedTenant.Suspend();
        var disabledTenant = new Tenant("Delta", "DELTA");
        disabledTenant.Disable();

        context.Tenants.AddRange(activeTenant1, activeTenant2, suspendedTenant, disabledTenant);

        // Add users
        var userManager = TestIdentityHelper.CreateUserManager(context);
        var superAdmin = new ApplicationUser
        {
            UserName = "admin@platform.com",
            Email = "admin@platform.com",
            PlatformRole = PlatformRole.SuperAdmin,
            IsActive = true
        };
        var normalActive = new ApplicationUser
        {
            UserName = "user1@tenant.com",
            Email = "user1@tenant.com",
            PlatformRole = PlatformRole.None,
            IsActive = true
        };
        var normalInactive = new ApplicationUser
        {
            UserName = "user2@tenant.com",
            Email = "user2@tenant.com",
            PlatformRole = PlatformRole.None,
            IsActive = false
        };

        await userManager.CreateAsync(superAdmin, "AdminPass123!");
        await userManager.CreateAsync(normalActive, "UserPass123!");
        await userManager.CreateAsync(normalInactive, "UserPass123!");

        var stats = await service.GetDashboardStatsAsync();

        Assert.Equal(4, stats.TotalTenants);
        Assert.Equal(2, stats.ActiveTenants);
        Assert.Equal(1, stats.SuspendedTenants);
        Assert.Equal(1, stats.DisabledTenants);
        Assert.Equal(3, stats.TotalUsers);
        Assert.Equal(2, stats.ActiveUsers);
        Assert.Equal(1, stats.PlatformUsers);
        Assert.NotEmpty(stats.RecentTenants);
        Assert.Equal(6, stats.TenantGrowthTrend.Count);
    }
}
