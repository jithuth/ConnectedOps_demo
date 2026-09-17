using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Dispatch;

public interface IDispatchService
{
    Task<DispatchDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<DispatchJobDto>> GetJobsPagedAsync(
        DispatchJobFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<DispatchJobDto?> GetJobByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DispatchJobDto> CreateJobAsync(
        CreateDispatchJobRequest request,
        CancellationToken cancellationToken = default);

    Task<DispatchJobDto> UpdateJobAsync(
        Guid id,
        UpdateDispatchJobRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> CancelJobAsync(
        Guid id,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DispatchRouteDto>> GetRoutesPagedAsync(
        DispatchRouteFilterRequest filter,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto?> GetRouteByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> CreateRouteAsync(
        CreateDispatchRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> UpdateRouteAsync(
        Guid id,
        UpdateDispatchRouteRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DispatchRouteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> StartRouteAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteRouteAsync(
        Guid id,
        decimal? actualDistanceKm = null,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> AddStopAsync(
        Guid routeId,
        AddRouteStopRequest request,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> RemoveStopAsync(
        Guid routeId,
        Guid stopId,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> ReorderStopsAsync(
        Guid routeId,
        List<Guid> stopIdsInOrder,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteDto> OptimizeRouteAsync(
        Guid routeId,
        OptimizeRouteStopsRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<DispatchRouteStopDto> UpdateStopStatusAsync(
        Guid stopId,
        UpdateRouteStopStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<ProofOfDeliveryDto> RecordProofOfDeliveryAsync(
        Guid jobId,
        RecordProofOfDeliveryRequest request,
        CancellationToken cancellationToken = default);

    Task<ProofOfDeliveryDto?> GetProofOfDeliveryByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}
