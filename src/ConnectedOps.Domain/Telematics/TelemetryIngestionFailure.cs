namespace ConnectedOps.Domain.Telematics;

public sealed class TelemetryIngestionFailure
{
    private TelemetryIngestionFailure()
    {
    }

    public TelemetryIngestionFailure(
        string deviceIdentifier,
        string provider,
        string reason,
        Guid? tenantId = null,
        string? traceId = null,
        string? payloadReference = null)
    {
        if (string.IsNullOrWhiteSpace(deviceIdentifier))
            throw new ArgumentException("Device identifier is required.", nameof(deviceIdentifier));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        DeviceIdentifier = deviceIdentifier.Trim();
        Provider = provider.Trim();
        Reason = reason.Trim();
        TraceId = traceId?.Trim() ?? Guid.NewGuid().ToString("N");
        PayloadReference = payloadReference?.Trim();
        ReceivedAtUtc = DateTime.UtcNow;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? TenantId { get; private set; }
    public string DeviceIdentifier { get; private set; } = null!;
    public string Provider { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public string TraceId { get; private set; } = null!;
    public string? PayloadReference { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
}
