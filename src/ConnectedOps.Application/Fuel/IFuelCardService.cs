using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public interface IFuelCardService
{
    Task<PagedResult<FuelCardListItemDto>> GetCardsPagedAsync(
        FuelCardQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FuelCardDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<FuelCardDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FuelCardDto> CreateAsync(
        CreateFuelCardRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelCardDto> UpdateAsync(
        Guid id,
        UpdateFuelCardRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelCardDto> SetStatusAsync(
        Guid id,
        FuelCardStatus status,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
