using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Identity;
using Xunit;

namespace ConnectedOps.Tests.Unit;

public sealed class PlatformRoleTests
{
    [Fact]
    public void PlatformRole_ContainsExpectedEnumValues()
    {
        Assert.Equal(0, (int)PlatformRole.None);
        Assert.Equal(1, (int)PlatformRole.SuperAdmin);
        Assert.Equal(2, (int)PlatformRole.PlatformAdmin);
        Assert.Equal(3, (int)PlatformRole.SupportAdmin);
        Assert.Equal(4, (int)PlatformRole.BillingAdmin);
        Assert.Equal(5, (int)PlatformRole.ReadOnlyAdmin);
    }

    [Fact]
    public void ApplicationUser_DefaultsToPlatformRoleNone()
    {
        var user = new ApplicationUser();

        Assert.Equal(PlatformRole.None, user.PlatformRole);
        Assert.True(user.IsActive);
        Assert.NotEqual(Guid.Empty, user.Id);
    }
}
