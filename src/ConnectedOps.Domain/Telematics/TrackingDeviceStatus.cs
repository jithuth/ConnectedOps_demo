namespace ConnectedOps.Domain.Telematics;

public enum TrackingDeviceStatus
{
    Pending = 1,
    Provisioned = 2,
    Online = 3,
    Offline = 4,
    Suspended = 5,
    Disabled = 6,
    Retired = 7,
    Faulted = 8
}
