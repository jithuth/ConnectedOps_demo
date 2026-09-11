namespace ConnectedOps.Application.Telematics;

public sealed record TelemetryValidationResult(
    bool IsValid,
    string? Reason = null);

public interface ITelemetryValidationService
{
    TelemetryValidationResult Validate(
        NormalizedTelemetryMessage message,
        int maxAcceptedFutureMinutes = 15,
        int maxAcceptedPastDays = 7);
}
