using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Optimization;

public interface IRouteOptimizationService
{
    Task<PagedResult<RouteOptimizationRunDto>> GetOptimizationRunsPagedAsync(
        OptimizationFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<RouteOptimizationRunDto?> GetOptimizationRunByIdAsync(
        Guid runId,
        CancellationToken cancellationToken = default);

    Task<RouteOptimizationRunDto> SolveVrpAsync(
        CreateOptimizationRunRequest request,
        CancellationToken cancellationToken = default);

    Task<RouteOptimizationRunDto> DispatchRunAsync(
        DispatchOptimizationRunRequest request,
        CancellationToken cancellationToken = default);
}
