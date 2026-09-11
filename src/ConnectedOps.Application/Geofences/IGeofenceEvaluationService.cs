namespace ConnectedOps.Application.Geofences;

public interface IGeofenceEvaluationService
{
    Task EvaluatePositionAsync(
        Guid tenantId,
        Guid vehicleId,
        double latitude,
        double longitude,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc,
        Guid? trackingDeviceId = null,
        Guid? telemetryRecordId = null,
        CancellationToken cancellationToken = default);
}
