namespace ConnectedOps.Application.Safety;

public interface ISafetyDashboardService
{
    Task<SafetyDashboardDto> GetDashboardMetricsAsync(CancellationToken cancellationToken = default);
}
