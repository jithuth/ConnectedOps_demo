namespace ConnectedOps.Application.Organization;

public interface IOrganizationHierarchyService
{
    Task<IReadOnlyCollection<OrganizationHierarchyNodeDto>> GetHierarchyAsync(
        CancellationToken cancellationToken = default);
}
