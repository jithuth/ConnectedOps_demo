namespace ConnectedOps.Application.Permissions;

public sealed record RolePermissionResult(
    Guid RoleId,
    string RoleName,
    string RoleCode,
    bool IsSystemRole,
    IReadOnlyCollection<PermissionListItem> Permissions);