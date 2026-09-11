using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class TrackingDevice : BaseEntity
{
    private TrackingDevice()
    {
    }

    public TrackingDevice(
        Guid tenantId,
        string deviceIdentifier,
        Guid providerId,
        Guid deviceTypeId,
        string? imei = null,
        string? serialNumber = null,
        string? name = null,
        string? model = null,
        string? manufacturer = null,
        string? firmwareVersion = null,
        string? simNumber = null,
        string? simIccid = null,
        string? phoneNumber = null,
        TrackingDeviceStatus status = TrackingDeviceStatus.Pending,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(deviceIdentifier))
            throw new ArgumentException("Device identifier is required.", nameof(deviceIdentifier));
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));
        if (deviceTypeId == Guid.Empty)
            throw new ArgumentException("DeviceTypeId is required.", nameof(deviceTypeId));

        TenantId = tenantId;
        DeviceIdentifier = deviceIdentifier.Trim();
        ProviderId = providerId;
        DeviceTypeId = deviceTypeId;
        IMEI = string.IsNullOrWhiteSpace(imei) ? null : imei.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim();
        FirmwareVersion = string.IsNullOrWhiteSpace(firmwareVersion) ? null : firmwareVersion.Trim();
        SIMNumber = string.IsNullOrWhiteSpace(simNumber) ? null : simNumber.Trim();
        SIMICCID = string.IsNullOrWhiteSpace(simIccid) ? null : simIccid.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();
        Status = status;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string DeviceIdentifier { get; private set; } = null!;
    public string? IMEI { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Name { get; private set; }

    public Guid ProviderId { get; private set; }
    public TrackingProvider Provider { get; private set; } = null!;

    public Guid DeviceTypeId { get; private set; }
    public TrackingDeviceType DeviceType { get; private set; } = null!;

    public string? Model { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? FirmwareVersion { get; private set; }

    public string? SIMNumber { get; private set; }
    public string? SIMICCID { get; private set; }
    public string? PhoneNumber { get; private set; }

    public TrackingDeviceStatus Status { get; private set; } = TrackingDeviceStatus.Pending;

    public DateTime? LastSeenAtUtc { get; private set; }
    public DateTime? LastTelemetryAtUtc { get; private set; }

    public double? LastKnownLatitude { get; private set; }
    public double? LastKnownLongitude { get; private set; }
    public decimal? LastKnownSpeedKph { get; private set; }
    public decimal? LastKnownHeadingDegrees { get; private set; }
    public bool? LastKnownIgnition { get; private set; }

    public int? BatteryLevelPercent { get; private set; }
    public decimal? ExternalPowerVoltage { get; private set; }
    public decimal? BatteryVoltage { get; private set; }
    public int? SignalStrength { get; private set; }

    public bool IsActive { get; private set; } = true;

    public void Update(
        string? name,
        Guid providerId,
        Guid deviceTypeId,
        string? imei,
        string? serialNumber,
        string? model,
        string? manufacturer,
        string? firmwareVersion,
        string? simNumber,
        string? simIccid,
        string? phoneNumber,
        Guid? userId = null)
    {
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));
        if (deviceTypeId == Guid.Empty)
            throw new ArgumentException("DeviceTypeId is required.", nameof(deviceTypeId));

        Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        ProviderId = providerId;
        DeviceTypeId = deviceTypeId;
        IMEI = string.IsNullOrWhiteSpace(imei) ? null : imei.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
        Manufacturer = string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer.Trim();
        FirmwareVersion = string.IsNullOrWhiteSpace(firmwareVersion) ? null : firmwareVersion.Trim();
        SIMNumber = string.IsNullOrWhiteSpace(simNumber) ? null : simNumber.Trim();
        SIMICCID = string.IsNullOrWhiteSpace(simIccid) ? null : simIccid.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        MarkUpdated(userId);
    }

    public void UpdateTelemetryState(
        double? latitude,
        double? longitude,
        decimal? speedKph,
        decimal? headingDegrees,
        bool? ignitionOn,
        int? batteryLevelPercent,
        decimal? externalPowerVoltage,
        decimal? batteryVoltage,
        int? signalStrength,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc)
    {
        LastSeenAtUtc = receivedAtUtc;
        LastTelemetryAtUtc = recordedAtUtc;

        if (latitude.HasValue) LastKnownLatitude = latitude.Value;
        if (longitude.HasValue) LastKnownLongitude = longitude.Value;
        if (speedKph.HasValue) LastKnownSpeedKph = speedKph.Value;
        if (headingDegrees.HasValue) LastKnownHeadingDegrees = headingDegrees.Value;
        if (ignitionOn.HasValue) LastKnownIgnition = ignitionOn.Value;
        if (batteryLevelPercent.HasValue) BatteryLevelPercent = batteryLevelPercent.Value;
        if (externalPowerVoltage.HasValue) ExternalPowerVoltage = externalPowerVoltage.Value;
        if (batteryVoltage.HasValue) BatteryVoltage = batteryVoltage.Value;
        if (signalStrength.HasValue) SignalStrength = signalStrength.Value;

        if (Status == TrackingDeviceStatus.Pending || Status == TrackingDeviceStatus.Offline)
        {
            Status = TrackingDeviceStatus.Online;
        }

        MarkUpdated();
    }

    public void ChangeStatus(TrackingDeviceStatus status, Guid? userId = null)
    {
        Status = status;
        MarkUpdated(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        if (Status == TrackingDeviceStatus.Disabled || Status == TrackingDeviceStatus.Suspended)
        {
            Status = TrackingDeviceStatus.Provisioned;
        }
        MarkUpdated(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        Status = TrackingDeviceStatus.Disabled;
        MarkUpdated(userId);
    }
}
