namespace ConnectedOps.Application.Vehicles;

public interface IVehicleMakeModelService
{
    // Makes
    Task<IReadOnlyCollection<VehicleMakeDto>> GetMakesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<VehicleMakeDto> GetMakeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleMakeDto> CreateMakeAsync(
        CreateVehicleMakeRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMakeDto> UpdateMakeAsync(
        Guid id,
        UpdateVehicleMakeRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMakeAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    // Models
    Task<IReadOnlyCollection<VehicleModelDto>> GetModelsByMakeIdAsync(
        Guid makeId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleModelDto>> GetAllModelsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    Task<VehicleModelDto> GetModelByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleModelDto> CreateModelAsync(
        CreateVehicleModelRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleModelDto> UpdateModelAsync(
        Guid id,
        UpdateVehicleModelRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteModelAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
