namespace ConnectedOps.Application.Vehicles;

public interface IVehicleOdometerService
{
    Task<IReadOnlyCollection<VehicleOdometerEntryDto>> GetOdometerHistoryAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<VehicleOdometerEntryDto> RecordOdometerAsync(
        Guid vehicleId,
        RecordVehicleOdometerRequest request,
        CancellationToken cancellationToken = default);
}
