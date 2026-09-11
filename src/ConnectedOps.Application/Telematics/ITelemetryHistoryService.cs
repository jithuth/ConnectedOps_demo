namespace ConnectedOps.Application.Telematics;

public interface ITelemetryHistoryService
{
    Task<TelemetryHistoryPagedResult> GetVehicleTelemetryHistoryAsync(
        Guid vehicleId,
        TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<TelemetryHistoryPagedResult> GetDeviceTelemetryHistoryAsync(
        Guid deviceId,
        TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
