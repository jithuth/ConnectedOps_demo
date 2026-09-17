using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.ColdChain;

public sealed class CargoSensorDevice : BaseEntity
{
    private CargoSensorDevice()
    {
    }

    public CargoSensorDevice(
        Guid tenantId,
        string sensorTagNumber,
        string compartmentName,
        Guid vehicleId,
        double minTargetTemperatureCelsius,
        double maxTargetTemperatureCelsius,
        int batteryLevelPercent = 100,
        string? macAddress = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sensorTagNumber))
            throw new ArgumentException("SensorTagNumber is required.", nameof(sensorTagNumber));
        if (string.IsNullOrWhiteSpace(compartmentName))
            throw new ArgumentException("CompartmentName is required.", nameof(compartmentName));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        SensorTagNumber = sensorTagNumber.Trim().ToUpperInvariant();
        CompartmentName = compartmentName.Trim();
        VehicleId = vehicleId;
        MinTargetTemperatureCelsius = minTargetTemperatureCelsius;
        MaxTargetTemperatureCelsius = maxTargetTemperatureCelsius;
        BatteryLevelPercent = Math.Clamp(batteryLevelPercent, 0, 100);
        MacAddress = macAddress?.Trim();
        IsActive = true;
        LastReadingAtUtc = DateTime.UtcNow;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string SensorTagNumber { get; private set; } = string.Empty;
    public string CompartmentName { get; private set; } = string.Empty;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public double MinTargetTemperatureCelsius { get; private set; }
    public double MaxTargetTemperatureCelsius { get; private set; }
    public int BatteryLevelPercent { get; private set; }
    public string? MacAddress { get; private set; }
    public bool IsActive { get; private set; }

    public double? CurrentTemperatureCelsius { get; private set; }
    public double? CurrentHumidityPercent { get; private set; }
    public bool CurrentDoorOpen { get; private set; }
    public DateTime LastReadingAtUtc { get; private set; }

    public void UpdateLatestTelemetry(double temp, double? humidity, bool doorOpen, int? battery)
    {
        CurrentTemperatureCelsius = temp;
        CurrentHumidityPercent = humidity;
        CurrentDoorOpen = doorOpen;
        if (battery.HasValue)
            BatteryLevelPercent = Math.Clamp(battery.Value, 0, 100);
        LastReadingAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void SetActive(bool active, Guid? updatedBy = null)
    {
        IsActive = active;
        MarkUpdated(updatedBy);
    }
}
