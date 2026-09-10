using ConnectedOps.Domain.Platform;
using Microsoft.AspNetCore.Authorization;

namespace ConnectedOps.Infrastructure.Authorization;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RequirePlatformRoleAttribute
    : AuthorizeAttribute
{
    public RequirePlatformRoleAttribute(
        PlatformRole role)
    {
        Policy =
            $"{PermissionPolicyProvider.PlatformRolePolicyPrefix}{role}";
    }
}
