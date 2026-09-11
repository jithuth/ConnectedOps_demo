namespace ConnectedOps.Application.Telematics;

public interface ITelemetryIngestionService
{
    Task<TelemetryIngestionResult> IngestAsync(
        NormalizedTelemetryMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryIngestionResult>> IngestBatchAsync(
        IReadOnlyList<NormalizedTelemetryMessage> messages,
        CancellationToken cancellationToken = default);
}
