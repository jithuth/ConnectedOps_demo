namespace ConnectedOps.Application.TenantRoles;

public interface ITenantRoleManagementService
{
    Task<IReadOnlyCollection<TenantRoleListItem>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    Task<TenantRoleDetailsResult> GetRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task<CreateTenantRoleResult> CreateAsync(
        CreateTenantRoleRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid roleId,
        UpdateTenantRoleRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);
}