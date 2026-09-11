namespace ConnectedOps.Application.Organization;

public interface IOrganizationProfileService
{
    Task<OrganizationProfileDto> GetProfileAsync(
        CancellationToken cancellationToken = default);

    Task<OrganizationProfileDto> UpdateProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken = default);
}
