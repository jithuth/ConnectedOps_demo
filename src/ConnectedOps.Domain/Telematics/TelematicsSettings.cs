using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class TelematicsSettings : BaseEntity
{
    private TelematicsSettings()
    {
    }

    public TelematicsSettings(
        Guid tenantId,
        int offlineThresholdMinutes = 5,
        int telemetryRetentionDays = 90,
        int maxAcceptedFutureMinutes = 15,
        int maxAcceptedPastDays = 7,
        decimal odometerUpdateThresholdKm = 1.0m,
        int odometerUpdateMinIntervalMinutes = 15)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        Update(
            offlineThresholdMinutes,
            telemetryRetentionDays,
            maxAcceptedFutureMinutes,
            maxAcceptedPastDays,
            odometerUpdateThresholdKm,
            odometerUpdateMinIntervalMinutes);
    }

    public Guid TenantId { get; private set; }
    public int OfflineThresholdMinutes { get; private set; } = 5;
    public int TelemetryRetentionDays { get; private set; } = 90;
    public int MaxAcceptedFutureMinutes { get; private set; } = 15;
    public int MaxAcceptedPastDays { get; private set; } = 7;
    public decimal OdometerUpdateThresholdKm { get; private set; } = 1.0m;
    public int OdometerUpdateMinIntervalMinutes { get; private set; } = 15;

    public void Update(
        int offlineThresholdMinutes,
        int telemetryRetentionDays,
        int maxAcceptedFutureMinutes,
        int maxAcceptedPastDays,
        decimal odometerUpdateThresholdKm,
        int odometerUpdateMinIntervalMinutes,
        Guid? userId = null)
    {
        if (offlineThresholdMinutes < 1)
            throw new ArgumentOutOfRangeException(nameof(offlineThresholdMinutes), "Offline threshold must be at least 1 minute.");
        if (telemetryRetentionDays < 1)
            throw new ArgumentOutOfRangeException(nameof(telemetryRetentionDays), "Retention days must be at least 1 day.");
        if (maxAcceptedFutureMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAcceptedFutureMinutes), "Max future minutes cannot be negative.");
        if (maxAcceptedPastDays < 1)
            throw new ArgumentOutOfRangeException(nameof(maxAcceptedPastDays), "Max past days must be at least 1 day.");
        if (odometerUpdateThresholdKm < 0)
            throw new ArgumentOutOfRangeException(nameof(odometerUpdateThresholdKm), "Odometer threshold cannot be negative.");
        if (odometerUpdateMinIntervalMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(odometerUpdateMinIntervalMinutes), "Odometer interval cannot be negative.");

        OfflineThresholdMinutes = offlineThresholdMinutes;
        TelemetryRetentionDays = telemetryRetentionDays;
        MaxAcceptedFutureMinutes = maxAcceptedFutureMinutes;
        MaxAcceptedPastDays = maxAcceptedPastDays;
        OdometerUpdateThresholdKm = odometerUpdateThresholdKm;
        OdometerUpdateMinIntervalMinutes = odometerUpdateMinIntervalMinutes;

        MarkUpdated(userId);
    }
}
