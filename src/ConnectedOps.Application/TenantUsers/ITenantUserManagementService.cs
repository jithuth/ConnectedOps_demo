namespace ConnectedOps.Application.TenantUsers;

public interface ITenantUserManagementService
{
    Task<IReadOnlyCollection<TenantUserListItem>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<TenantUserDetailsResult> GetUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task UpdateRolesAsync(
        Guid tenantUserId,
        UpdateTenantUserRolesRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task DeactivateAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);
}