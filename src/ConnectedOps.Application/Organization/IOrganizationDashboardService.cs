namespace ConnectedOps.Application.Organization;

public interface IOrganizationDashboardService
{
    Task<OrganizationDashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default);
}
