using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public interface IFleetOperationalExceptionService
{
    Task<PagedResult<FleetOperationalExceptionDto>> GetExceptionsPagedAsync(
        OperationalExceptionQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<FleetOperationalExceptionDto> GetExceptionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FleetOperationalExceptionDto> CreateExceptionAsync(
        CreateOperationalExceptionRequest request,
        CancellationToken cancellationToken = default);

    Task<FleetOperationalExceptionDto> ResolveExceptionAsync(
        Guid id,
        ResolveOperationalExceptionRequest request,
        CancellationToken cancellationToken = default);

    Task<FleetOperationalExceptionDto> DismissExceptionAsync(
        Guid id,
        DismissOperationalExceptionRequest request,
        CancellationToken cancellationToken = default);
}
