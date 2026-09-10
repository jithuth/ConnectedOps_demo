namespace ConnectedOps.Application.TenantUsers;

public sealed class UpdateTenantUserRolesRequest
{
    public IReadOnlyCollection<Guid> RoleIds { get; init; }
        = Array.Empty<Guid>();
}