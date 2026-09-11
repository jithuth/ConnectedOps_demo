namespace ConnectedOps.Application.Fuel;

public interface IFuelTypeService
{
    Task<IReadOnlyCollection<FuelTypeDefinitionDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<FuelTypeDefinitionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FuelTypeDefinitionDto> CreateAsync(
        CreateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelTypeDefinitionDto> UpdateAsync(
        Guid id,
        UpdateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
