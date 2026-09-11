namespace ConnectedOps.Domain.Telematics;

public enum TelemetryEventType
{
    Periodic = 1,
    IgnitionOn = 2,
    IgnitionOff = 3,
    MovementStart = 4,
    MovementStop = 5,
    Heartbeat = 6,
    Alarm = 7,
    CommandResponse = 8,
    Custom = 99
}
