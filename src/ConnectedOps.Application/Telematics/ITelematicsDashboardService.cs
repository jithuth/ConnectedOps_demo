namespace ConnectedOps.Application.Telematics;

public interface ITelematicsDashboardService
{
    Task<TelematicsDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}
