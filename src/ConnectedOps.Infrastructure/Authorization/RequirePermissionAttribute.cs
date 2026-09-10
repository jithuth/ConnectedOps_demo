using Microsoft.AspNetCore.Authorization;

namespace ConnectedOps.Infrastructure.Authorization;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RequirePermissionAttribute
    : AuthorizeAttribute
{
    public RequirePermissionAttribute(
        string permission)
    {
        Policy =
            $"{PermissionPolicyProvider.PolicyPrefix}{permission}";
    }
}