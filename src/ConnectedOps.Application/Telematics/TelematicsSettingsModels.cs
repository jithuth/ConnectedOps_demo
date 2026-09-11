namespace ConnectedOps.Application.Telematics;

public sealed record TelematicsSettingsDto(
    Guid Id,
    Guid TenantId,
    int OfflineThresholdMinutes,
    int TelemetryRetentionDays,
    int MaxAcceptedFutureMinutes,
    int MaxAcceptedPastDays,
    decimal OdometerUpdateThresholdKm,
    int odometerUpdateMinIntervalMinutes,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpdateTelematicsSettingsRequest(
    int OfflineThresholdMinutes,
    int TelemetryRetentionDays,
    int MaxAcceptedFutureMinutes,
    int MaxAcceptedPastDays,
    decimal OdometerUpdateThresholdKm,
    int OdometerUpdateMinIntervalMinutes);
