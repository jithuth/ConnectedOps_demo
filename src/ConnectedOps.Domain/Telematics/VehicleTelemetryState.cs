using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Telematics;

public sealed class VehicleTelemetryState
{
    private VehicleTelemetryState()
    {
    }

    public VehicleTelemetryState(
        Guid vehicleId,
        Guid tenantId,
        Guid trackingDeviceId,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc,
        double latitude,
        double longitude,
        double? altitudeMeters = null,
        decimal? speedKph = null,
        decimal? headingDegrees = null,
        bool? ignitionOn = null,
        decimal? odometerKm = null,
        decimal? engineHours = null,
        decimal? fuelLevelPercent = null,
        decimal? batteryVoltage = null,
        decimal? externalPowerVoltage = null,
        int? signalStrength = null)
    {
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (trackingDeviceId == Guid.Empty)
            throw new ArgumentException("TrackingDeviceId is required.", nameof(trackingDeviceId));

        VehicleId = vehicleId;
        TenantId = tenantId;
        TrackingDeviceId = trackingDeviceId;
        RecordedAtUtc = recordedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        Latitude = latitude;
        Longitude = longitude;
        AltitudeMeters = altitudeMeters;
        SpeedKph = speedKph;
        HeadingDegrees = headingDegrees;
        IgnitionOn = ignitionOn;
        OdometerKm = odometerKm;
        EngineHours = engineHours;
        FuelLevelPercent = fuelLevelPercent;
        BatteryVoltage = batteryVoltage;
        ExternalPowerVoltage = externalPowerVoltage;
        SignalStrength = signalStrength;
        LastUpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid TenantId { get; private set; }

    public Guid TrackingDeviceId { get; private set; }
    public TrackingDevice TrackingDevice { get; private set; } = null!;

    public DateTime RecordedAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public double? AltitudeMeters { get; private set; }

    public decimal? SpeedKph { get; private set; }
    public decimal? HeadingDegrees { get; private set; }

    public bool? IgnitionOn { get; private set; }

    public decimal? OdometerKm { get; private set; }
    public decimal? EngineHours { get; private set; }

    public decimal? FuelLevelPercent { get; private set; }

    public decimal? BatteryVoltage { get; private set; }
    public decimal? ExternalPowerVoltage { get; private set; }
    public int? SignalStrength { get; private set; }

    public DateTime LastUpdatedAtUtc { get; private set; } = DateTime.UtcNow;

    public bool UpdateIfNewer(
        Guid trackingDeviceId,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc,
        double latitude,
        double longitude,
        double? altitudeMeters,
        decimal? speedKph,
        decimal? headingDegrees,
        bool? ignitionOn,
        decimal? odometerKm,
        decimal? engineHours,
        decimal? fuelLevelPercent,
        decimal? batteryVoltage,
        decimal? externalPowerVoltage,
        int? signalStrength)
    {
        // Concurrency safeguard: stale telemetry must not overwrite newer current-state projection
        if (recordedAtUtc < RecordedAtUtc)
        {
            return false;
        }

        TrackingDeviceId = trackingDeviceId;
        RecordedAtUtc = recordedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        Latitude = latitude;
        Longitude = longitude;
        AltitudeMeters = altitudeMeters;
        SpeedKph = speedKph;
        HeadingDegrees = headingDegrees;
        IgnitionOn = ignitionOn;
        if (odometerKm.HasValue && (!OdometerKm.HasValue || odometerKm.Value >= OdometerKm.Value))
        {
            OdometerKm = odometerKm.Value;
        }
        if (engineHours.HasValue) EngineHours = engineHours.Value;
        if (fuelLevelPercent.HasValue) FuelLevelPercent = fuelLevelPercent.Value;
        if (batteryVoltage.HasValue) BatteryVoltage = batteryVoltage.Value;
        if (externalPowerVoltage.HasValue) ExternalPowerVoltage = externalPowerVoltage.Value;
        if (signalStrength.HasValue) SignalStrength = signalStrength.Value;
        LastUpdatedAtUtc = DateTime.UtcNow;

        return true;
    }
}
