namespace ConnectedOps.Application.Vehicles;

public interface IVehicleCategoryService
{
    Task<IReadOnlyCollection<VehicleCategoryDto>> GetCategoriesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<VehicleCategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleCategoryDto> CreateCategoryAsync(
        CreateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleCategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeactivateCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
