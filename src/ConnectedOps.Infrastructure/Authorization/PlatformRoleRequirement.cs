using ConnectedOps.Domain.Platform;
using Microsoft.AspNetCore.Authorization;

namespace ConnectedOps.Infrastructure.Authorization;

public sealed class PlatformRoleRequirement : IAuthorizationRequirement
{
    public PlatformRoleRequirement(PlatformRole role)
    {
        Role = role;
    }

    public PlatformRole Role { get; }
}
