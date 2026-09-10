namespace ConnectedOps.Application.TenantRoles;

public sealed record TenantRoleListItem(
    Guid Id,
    string Name,
    string Code,
    bool IsSystemRole,
    int UserCount);