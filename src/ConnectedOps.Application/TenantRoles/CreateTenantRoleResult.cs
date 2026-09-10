namespace ConnectedOps.Application.TenantRoles;

public sealed record CreateTenantRoleResult(
    Guid Id,
    string Name,
    string Code,
    bool IsSystemRole);