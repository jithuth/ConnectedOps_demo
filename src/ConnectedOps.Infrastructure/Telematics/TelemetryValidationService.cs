using ConnectedOps.Application.Telematics;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelemetryValidationService : ITelemetryValidationService
{
    public TelemetryValidationResult Validate(
        NormalizedTelemetryMessage message,
        int maxAcceptedFutureMinutes = 15,
        int maxAcceptedPastDays = 7)
    {
        if (string.IsNullOrWhiteSpace(message.DeviceIdentifier))
        {
            return new TelemetryValidationResult(false, "DeviceIdentifier cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(message.Provider))
        {
            return new TelemetryValidationResult(false, "Provider cannot be empty.");
        }

        // Coordinate checks
        if (message.Latitude.HasValue && (message.Latitude.Value < -90.0 || message.Latitude.Value > 90.0))
        {
            return new TelemetryValidationResult(false, $"Latitude {message.Latitude.Value} is out of valid range [-90, +90].");
        }

        if (message.Longitude.HasValue && (message.Longitude.Value < -180.0 || message.Longitude.Value > 180.0))
        {
            return new TelemetryValidationResult(false, $"Longitude {message.Longitude.Value} is out of valid range [-180, +180].");
        }

        // Speed checks
        if (message.SpeedKph.HasValue && message.SpeedKph.Value < 0)
        {
            return new TelemetryValidationResult(false, $"Speed {message.SpeedKph.Value} cannot be negative.");
        }

        // Heading checks
        if (message.HeadingDegrees.HasValue && (message.HeadingDegrees.Value < 0 || message.HeadingDegrees.Value > 360))
        {
            return new TelemetryValidationResult(false, $"Heading {message.HeadingDegrees.Value} is out of valid range [0, 360].");
        }

        // Time checks
        var now = DateTime.UtcNow;
        if (message.RecordedAtUtc > now.AddMinutes(maxAcceptedFutureMinutes))
        {
            return new TelemetryValidationResult(false, $"Recorded timestamp {message.RecordedAtUtc:u} is in the future beyond allowed tolerance ({maxAcceptedFutureMinutes} min).");
        }

        if (message.RecordedAtUtc < now.AddDays(-maxAcceptedPastDays))
        {
            return new TelemetryValidationResult(false, $"Recorded timestamp {message.RecordedAtUtc:u} is older than allowed retention policy ({maxAcceptedPastDays} days).");
        }

        return new TelemetryValidationResult(true);
    }
}
