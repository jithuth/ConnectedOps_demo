namespace ConnectedOps.Application.Vehicles;

public interface IVehicleService
{
    Task<PagedResult<VehicleListItemDto>> GetVehiclesPagedAsync(
        VehicleQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> GetVehicleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> CreateVehicleAsync(
        CreateVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> UpdateVehicleAsync(
        Guid id,
        UpdateVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> ChangeStatusAsync(
        Guid id,
        ChangeVehicleStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> AssignBranchAsync(
        Guid id,
        AssignVehicleBranchRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleDetailDto> AssignLocationAsync(
        Guid id,
        AssignVehicleLocationRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleSpecificationDto> UpsertSpecificationAsync(
        Guid id,
        UpsertVehicleSpecificationRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleRegistrationDto> AddRegistrationAsync(
        Guid id,
        CreateVehicleRegistrationRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleNoteDto> AddNoteAsync(
        Guid id,
        CreateVehicleNoteRequest request,
        CancellationToken cancellationToken = default);

    Task SetPrimaryImageAsync(
        Guid id,
        string? objectKey,
        CancellationToken cancellationToken = default);

    Task DeleteVehicleAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
