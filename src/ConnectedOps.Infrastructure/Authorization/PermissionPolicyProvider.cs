using ConnectedOps.Domain.Platform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider
    : DefaultAuthorizationPolicyProvider
{
    public const string PolicyPrefix =
        "Permission:";

    public const string PlatformRolePolicyPrefix =
        "PlatformRole:";

    public PermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?>
        GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(
                PolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var permission =
                policyName[
                    PolicyPrefix.Length..];

            var policy =
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new PermissionRequirement(
                            permission))
                    .Build();

            return Task.FromResult<
                AuthorizationPolicy?>(
                    policy);
        }

        if (policyName.StartsWith(
                PlatformRolePolicyPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var roleString =
                policyName[
                    PlatformRolePolicyPrefix.Length..];

            if (Enum.TryParse<PlatformRole>(
                    roleString,
                    ignoreCase: true,
                    out var role))
            {
                var policy =
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddRequirements(
                            new PlatformRoleRequirement(
                                role))
                        .Build();

                return Task.FromResult<
                    AuthorizationPolicy?>(
                        policy);
            }
        }

        return base.GetPolicyAsync(
            policyName);
    }
}