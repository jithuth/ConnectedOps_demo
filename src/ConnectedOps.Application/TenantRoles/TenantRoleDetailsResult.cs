namespace ConnectedOps.Application.TenantRoles;

public sealed record TenantRoleDetailsResult(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    bool IsSystemRole,
    int UserCount,
    IReadOnlyCollection<string> Permissions);