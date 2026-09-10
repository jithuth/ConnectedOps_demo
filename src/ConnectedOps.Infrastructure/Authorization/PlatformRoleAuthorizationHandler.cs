using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;

namespace ConnectedOps.Infrastructure.Authorization;

public sealed class PlatformRoleAuthorizationHandler
    : AuthorizationHandler<PlatformRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformRoleRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var platformRoleClaim = context.User
            .FindFirst(ConnectedOpsClaimTypes.PlatformRole)?
            .Value;

        if (string.IsNullOrWhiteSpace(platformRoleClaim))
        {
            return Task.CompletedTask;
        }

        if (!Enum.TryParse<PlatformRole>(platformRoleClaim, ignoreCase: true, out var userRole))
        {
            return Task.CompletedTask;
        }

        if (userRole == PlatformRole.None)
        {
            return Task.CompletedTask;
        }

        // SuperAdmin satisfies any platform requirement.
        // Other platform roles satisfy requirements matching their role.
        if (userRole == PlatformRole.SuperAdmin || userRole == requirement.Role)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
