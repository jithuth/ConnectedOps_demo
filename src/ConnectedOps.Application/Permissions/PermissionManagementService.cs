namespace ConnectedOps.Application.Permissions;

public interface IPermissionManagementService
{
    Task<IReadOnlyCollection<PermissionListItem>> GetPermissionsAsync(
        CancellationToken cancellationToken = default);

    Task<RolePermissionResult> GetRolePermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task UpdateRolePermissionsAsync(
        Guid roleId,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken = default);
}