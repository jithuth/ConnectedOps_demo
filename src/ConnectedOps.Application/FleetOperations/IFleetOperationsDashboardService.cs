namespace ConnectedOps.Application.FleetOperations;

public interface IFleetOperationsDashboardService
{
    Task<FleetOperationsDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<FleetOperationsBoardDto> GetBoardAsync(
        FleetAvailabilityFilter? filter = null,
        CancellationToken cancellationToken = default);
}
