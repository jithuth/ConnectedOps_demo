using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Geofences;

public sealed class GeofenceEvent : BaseEntity
{
    private GeofenceEvent()
    {
    }

    public GeofenceEvent(
        Guid tenantId,
        Guid vehicleId,
        Guid geofenceId,
        GeofenceEventType eventType,
        DateTime occurredAtUtc,
        DateTime receivedAtUtc,
        double latitude,
        double longitude,
        Guid? trackingDeviceId = null,
        Guid? driverId = null,
        Guid? telemetryRecordId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (geofenceId == Guid.Empty)
            throw new ArgumentException("GeofenceId is required.", nameof(geofenceId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        GeofenceId = geofenceId;
        EventType = eventType;
        OccurredAtUtc = occurredAtUtc;
        ReceivedAtUtc = receivedAtUtc;
        Latitude = latitude;
        Longitude = longitude;
        TrackingDeviceId = trackingDeviceId;
        DriverId = driverId;
        TelemetryRecordId = telemetryRecordId;
    }

    public Guid TenantId { get; private set; }

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid GeofenceId { get; private set; }
    public Geofence Geofence { get; private set; } = null!;

    public Guid? TrackingDeviceId { get; private set; }
    public TrackingDevice? TrackingDevice { get; private set; }

    public GeofenceEventType EventType { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }
    public DateTime ReceivedAtUtc { get; private set; }

    public double Latitude { get; private set; }
    public double Longitude { get; private set; }

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? TelemetryRecordId { get; private set; }
    public TelemetryRecord? TelemetryRecord { get; private set; }
}
