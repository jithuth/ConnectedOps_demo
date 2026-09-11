namespace ConnectedOps.Application.Vehicles;

public interface IVehicleDashboardService
{
    Task<VehicleDashboardStatsDto> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default);
}
