using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public interface IFleetActivityTimelineService
{
    Task<PagedResult<FleetActivityItemDto>> GetActivityTimelinePagedAsync(
        FleetActivityQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
