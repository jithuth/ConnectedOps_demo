using ConnectedOps.Application.Telematics;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Telematics.Providers;

public sealed class TeltonikaTelematicsProvider : ITelematicsProvider
{
    private readonly ILogger<TeltonikaTelematicsProvider> _logger;

    public TeltonikaTelematicsProvider(ILogger<TeltonikaTelematicsProvider> logger)
    {
        _logger = logger;
    }

    public string ProviderCode => "Teltonika";

    public bool CanHandle(string providerCode)
    {
        return string.Equals(providerCode, "Teltonika", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(providerCode, "TELTONIKA", StringComparison.OrdinalIgnoreCase);
    }

    public Task<IReadOnlyList<NormalizedTelemetryMessage>> ParseBinaryPayloadAsync(
        byte[] payload,
        string? deviceIdentifier = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var messages = TeltonikaCodec8Parser.Parse(payload, deviceIdentifier);
            return Task.FromResult(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Teltonika binary payload for device {DeviceIdentifier}", deviceIdentifier);
            return Task.FromResult<IReadOnlyList<NormalizedTelemetryMessage>>([]);
        }
    }

    public Task<bool> SendCommandAsync(
        TrackingDeviceDto device,
        DeviceCommandDto command,
        CancellationToken cancellationToken = default)
    {
        // Foundation only: encode safe command structure (e.g. Ping, RequestPosition, RequestDeviceInfo)
        _logger.LogInformation(
            "Prepared safe command {CommandType} for Teltonika device {DeviceIdentifier}",
            command.CommandType,
            device.DeviceIdentifier);

        // Command foundation queued/sent simulation
        return Task.FromResult(true);
    }
}
