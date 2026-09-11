namespace ConnectedOps.Application.FleetOperations;

public interface IVehicleHandoverService
{
    Task<IReadOnlyCollection<VehicleHandoverDto>> GetHandoversAsync(
        Guid? vehicleId = null,
        Guid? driverId = null,
        CancellationToken cancellationToken = default);

    Task<VehicleHandoverDto> GetHandoverByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleHandoverDto> CreateHandoverAsync(
        CreateVehicleHandoverRequest request,
        CancellationToken cancellationToken = default);
}
