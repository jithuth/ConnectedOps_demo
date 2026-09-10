using Microsoft.AspNetCore.Authorization;

namespace ConnectedOps.Infrastructure.Authorization;

public sealed class PermissionRequirement
    : IAuthorizationRequirement
{
    public PermissionRequirement(
        string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException(
                "Permission is required.",
                nameof(permission));
        }

        Permission = permission;
    }

    public string Permission { get; }
}