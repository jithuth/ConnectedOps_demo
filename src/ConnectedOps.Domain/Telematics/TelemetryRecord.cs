using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Telematics;

public sealed class TelemetryRecord
{
    private TelemetryRecord()
    {
    }

    public TelemetryRecord(
        Guid tenantId,
        Guid trackingDeviceId,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc,
        string sourceProvider,
        Guid? vehicleId = null,
        double? latitude = null,
        double? longitude = null,
        double? altitudeMeters = null,
        decimal? speedKph = null,
        decimal? headingDegrees = null,
        bool? ignitionOn = null,
        decimal? odometerKm = null,
        decimal? engineHours = null,
        decimal? fuelLevelPercent = null,
        decimal? fuelVolumeLiters = null,
        decimal? batteryVoltage = null,
        decimal? externalPowerVoltage = null,
        int? gsmSignal = null,
        int? gpsSatellites = null,
        decimal? temperatureCelsius = null,
        bool? digitalInput1 = null,
        bool? digitalInput2 = null,
        int? rawEventCode = null,
        TelemetryEventType eventType = TelemetryEventType.Periodic,
        string? providerMessageId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (trackingDeviceId == Guid.Empty)
            throw new ArgumentException("TrackingDeviceId is required.", nameof(trackingDeviceId));
        if (string.IsNullOrWhiteSpace(sourceProvider))
            throw new ArgumentException("SourceProvider is required.", nameof(sourceProvider));

        Id = Guid.NewGuid();
        TenantId = tenantId;
        TrackingDeviceId = trackingDeviceId;
        VehicleId = vehicleId;
        RecordedAtUtc = recordedAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        SourceProvider = sourceProvider.Trim();
        Latitude = latitude;
        Longitude = longitude;
        AltitudeMeters = altitudeMeters;
        SpeedKph = speedKph;
        HeadingDegrees = headingDegrees;
        IgnitionOn = ignitionOn;
        OdometerKm = odometerKm;
        EngineHours = engineHours;
        FuelLevelPercent = fuelLevelPercent;
        FuelVolumeLiters = fuelVolumeLiters;
        BatteryVoltage = batteryVoltage;
        ExternalPowerVoltage = externalPowerVoltage;
        GsmSignal = gsmSignal;
        GpsSatellites = gpsSatellites;
        TemperatureCelsius = temperatureCelsius;
        DigitalInput1 = digitalInput1;
        DigitalInput2 = digitalInput2;
        RawEventCode = rawEventCode;
        EventType = eventType;
        ProviderMessageId = providerMessageId?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid TenantId { get; private set; }

    public Guid TrackingDeviceId { get; private set; }
    public TrackingDevice TrackingDevice { get; private set; } = null!;

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public DateTime RecordedAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? AltitudeMeters { get; private set; }

    public decimal? SpeedKph { get; private set; }
    public decimal? HeadingDegrees { get; private set; }

    public bool? IgnitionOn { get; private set; }

    public decimal? OdometerKm { get; private set; }
    public decimal? EngineHours { get; private set; }

    public decimal? FuelLevelPercent { get; private set; }
    public decimal? FuelVolumeLiters { get; private set; }

    public decimal? BatteryVoltage { get; private set; }
    public decimal? ExternalPowerVoltage { get; private set; }

    public int? GsmSignal { get; private set; }
    public int? GpsSatellites { get; private set; }

    public decimal? TemperatureCelsius { get; private set; }

    public bool? DigitalInput1 { get; private set; }
    public bool? DigitalInput2 { get; private set; }

    public int? RawEventCode { get; private set; }
    public TelemetryEventType EventType { get; private set; } = TelemetryEventType.Periodic;

    public string SourceProvider { get; private set; } = null!;
    public string? ProviderMessageId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
}
