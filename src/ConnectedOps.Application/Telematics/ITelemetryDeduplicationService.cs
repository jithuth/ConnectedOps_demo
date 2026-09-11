namespace ConnectedOps.Application.Telematics;

public interface ITelemetryDeduplicationService
{
    Task<bool> IsDuplicateAsync(
        Guid tenantId,
        Guid trackingDeviceId,
        NormalizedTelemetryMessage message,
        CancellationToken cancellationToken = default);
}
