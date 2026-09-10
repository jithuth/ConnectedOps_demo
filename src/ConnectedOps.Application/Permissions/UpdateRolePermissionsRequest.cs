namespace ConnectedOps.Application.Permissions;

public sealed class UpdateRolePermissionsRequest
{
    public IReadOnlyCollection<Guid> PermissionIds { get; init; }
        = Array.Empty<Guid>();
}