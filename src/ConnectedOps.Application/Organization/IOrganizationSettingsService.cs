namespace ConnectedOps.Application.Organization;

public interface IOrganizationSettingsService
{
    Task<OrganizationSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<OrganizationSettingsDto> UpdateSettingsAsync(
        UpdateOrganizationSettingsRequest request,
        CancellationToken cancellationToken = default);
}
