namespace ConnectedOps.Application.Telematics;

public interface ITelematicsProvider
{
    string ProviderCode { get; }

    bool CanHandle(string providerCode);

    Task<IReadOnlyList<NormalizedTelemetryMessage>> ParseBinaryPayloadAsync(
        byte[] payload,
        string? deviceIdentifier = null,
        CancellationToken cancellationToken = default);

    Task<bool> SendCommandAsync(
        TrackingDeviceDto device,
        DeviceCommandDto command,
        CancellationToken cancellationToken = default);
}
