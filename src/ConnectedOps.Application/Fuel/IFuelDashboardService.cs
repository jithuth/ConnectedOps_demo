namespace ConnectedOps.Application.Fuel;

public interface IFuelDashboardService
{
    Task<FuelDashboardDto> GetDashboardMetricsAsync(
        CancellationToken cancellationToken = default);
}
