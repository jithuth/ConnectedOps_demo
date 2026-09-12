namespace ConnectedOps.Application.Compliance;

public interface IComplianceDashboardService
{
    Task<ComplianceDashboardDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}
