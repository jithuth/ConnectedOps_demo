namespace ConnectedOps.Application.TenantUsers;

public sealed record TenantUserRoleResult(
    Guid RoleId,
    string Name,
    string Code);