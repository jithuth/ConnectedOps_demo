using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.ColdChain;

public sealed class CargoTelemetryReading : BaseEntity
{
    private CargoTelemetryReading()
    {
    }

    public CargoTelemetryReading(
        Guid tenantId,
        Guid cargoSensorDeviceId,
        DateTime recordedAtUtc,
        double temperatureCelsius,
        double? humidityPercent = null,
        bool doorOpen = false,
        ReeferMode reeferMode = ReeferMode.Cooling,
        double? setpointTemperatureCelsius = null,
        double? latitude = null,
        double? longitude = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (cargoSensorDeviceId == Guid.Empty)
            throw new ArgumentException("CargoSensorDeviceId is required.", nameof(cargoSensorDeviceId));

        TenantId = tenantId;
        CargoSensorDeviceId = cargoSensorDeviceId;
        RecordedAtUtc = recordedAtUtc;
        TemperatureCelsius = temperatureCelsius;
        HumidityPercent = humidityPercent;
        DoorOpen = doorOpen;
        ReeferMode = reeferMode;
        SetpointTemperatureCelsius = setpointTemperatureCelsius;
        Latitude = latitude;
        Longitude = longitude;
    }

    public Guid TenantId { get; private set; }
    public Guid CargoSensorDeviceId { get; private set; }
    public CargoSensorDevice CargoSensorDevice { get; set; } = null!;

    public DateTime RecordedAtUtc { get; private set; }
    public double TemperatureCelsius { get; private set; }
    public double? HumidityPercent { get; private set; }
    public bool DoorOpen { get; private set; }
    public ReeferMode ReeferMode { get; private set; }
    public double? SetpointTemperatureCelsius { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
}
