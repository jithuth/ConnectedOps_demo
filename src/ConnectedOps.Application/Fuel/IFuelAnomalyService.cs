using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public interface IFuelAnomalyService
{
    Task<PagedResult<FuelAnomalyDto>> GetAnomaliesPagedAsync(
        FuelAnomalyQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FuelAnomalyDto>> EvaluateTransactionAnomaliesAsync(
        Guid fuelTransactionId,
        CancellationToken cancellationToken = default);

    Task<FuelAnomalyDto> ResolveAsync(
        Guid id,
        ResolveFuelAnomalyRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelAnomalyDto> DismissAsync(
        Guid id,
        DismissFuelAnomalyRequest request,
        CancellationToken cancellationToken = default);
}
