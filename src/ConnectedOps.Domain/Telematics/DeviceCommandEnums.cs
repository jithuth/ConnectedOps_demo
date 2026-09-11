namespace ConnectedOps.Domain.Telematics;

public enum DeviceCommandType
{
    Ping = 1,
    RequestPosition = 2,
    RequestDeviceInfo = 3,
    Custom = 99
}

public enum DeviceCommandStatus
{
    Pending = 1,
    Sent = 2,
    Acknowledged = 3,
    Failed = 4,
    Expired = 5,
    Cancelled = 6
}
