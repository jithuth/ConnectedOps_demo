using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.TollsAndFines;

public interface ITollAndFineService
{
    Task<TollsAndFinesDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<TollTransactionDto>> GetTollsPagedAsync(
        TollFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TrafficViolationDto>> GetViolationsPagedAsync(
        ViolationFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<TollTransactionDto> CreateTollAsync(
        CreateTollTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficViolationDto> CreateViolationAsync(
        CreateTrafficViolationRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficViolationDto> AssignLiabilityAsync(
        Guid violationId,
        AssignViolationLiabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficViolationDto> DisputeViolationAsync(
        Guid violationId,
        DisputeViolationRequest request,
        CancellationToken cancellationToken = default);

    Task<TrafficViolationDto> SettleViolationAsync(
        Guid violationId,
        SettleViolationRequest request,
        CancellationToken cancellationToken = default);

    Task<int> AutoMatchDriverLiabilityAsync(
        CancellationToken cancellationToken = default);
}
