using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Fuel;

public interface IFuelStationService
{
    Task<PagedResult<FuelStationListItemDto>> GetStationsPagedAsync(
        FuelStationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<FuelStationDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<FuelStationDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FuelStationDto> CreateAsync(
        CreateFuelStationRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelStationDto> UpdateAsync(
        Guid id,
        UpdateFuelStationRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
