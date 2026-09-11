namespace ConnectedOps.Application.Maintenance;

public interface IMaintenanceDashboardService
{
    Task<MaintenanceDashboardDto> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default);
}
