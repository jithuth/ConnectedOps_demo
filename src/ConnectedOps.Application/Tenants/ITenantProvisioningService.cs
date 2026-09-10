namespace ConnectedOps.Application.Tenants;

public interface ITenantProvisioningService
{
    Task<CreateTenantResult> CreateAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default);
}