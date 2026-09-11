namespace ConnectedOps.Application.Drivers;

public interface IDriverDashboardService
{
    Task<DriverDashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default);
}
