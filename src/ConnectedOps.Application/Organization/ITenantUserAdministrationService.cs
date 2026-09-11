namespace ConnectedOps.Application.Organization;

public interface ITenantUserAdministrationService
{
    Task<IReadOnlyCollection<TenantUserAdminDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<TenantUserAdminDto> GetUserByIdAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task<TenantUserAdminDto> CreateUserAsync(
        CreateTenantUserAdminRequest request,
        CancellationToken cancellationToken = default);

    Task<TenantUserAdminDto> UpdateUserAsync(
        Guid tenantUserId,
        UpdateTenantUserAdminRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);

    Task RemoveUserAsync(
        Guid tenantUserId,
        CancellationToken cancellationToken = default);
}
