namespace ConnectedOps.Application.Telematics;

public interface IVehicleTelemetryStateService
{
    Task<VehicleTelemetryStateDto?> GetVehicleTelemetryStateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<LiveVehicleTrackingDto>> GetLiveFleetTrackingAsync(
        LiveTrackingQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
