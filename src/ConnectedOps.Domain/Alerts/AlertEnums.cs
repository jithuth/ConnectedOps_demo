namespace ConnectedOps.Domain.Alerts;

public enum AlertSeverity
{
    Info = 1,
    Warning = 2,
    Critical = 3,
    Urgent = 4
}

public enum AlertStatus
{
    Triggered = 1,
    Acknowledged = 2,
    Assigned = 3,
    Resolved = 4,
    Dismissed = 5
}

public enum AlertSourceType
{
    Telemetry = 1,
    Geofence = 2,
    Safety = 3,
    Dispatch = 4,
    ColdChain = 5,
    DvirDefect = 6,
    TollsAndFines = 7,
    Maintenance = 8
}

public enum ConditionOperator
{
    GreaterThan = 1,
    LessThan = 2,
    Equals = 3,
    NotEquals = 4,
    Contains = 5
}

public enum NotificationChannelType
{
    InApp = 1,
    Email = 2,
    Sms = 3,
    Webhook = 4,
    WhatsApp = 5,
    Telegram = 6
}
