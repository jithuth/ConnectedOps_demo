namespace ConnectedOps.Application.Organization;

public interface ILocationService
{
    Task<IReadOnlyCollection<LocationListItemDto>> GetLocationsAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default);

    Task<LocationDto> GetLocationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LocationDto> CreateLocationAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<LocationDto> UpdateLocationAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteLocationAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
