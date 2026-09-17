namespace ConnectedOps.Domain.Dispatch;

public enum DispatchJobType
{
    Delivery = 1,
    Pickup = 2,
    Service = 3,
    Inspection = 4
}

public enum DispatchJobPriority
{
    Low = 1,
    Standard = 2,
    High = 3,
    Urgent = 4
}

public enum DispatchJobStatus
{
    Draft = 1,
    Unassigned = 2,
    Scheduled = 3,
    Dispatched = 4,
    InProgress = 5,
    Completed = 6,
    Failed = 7,
    Cancelled = 8
}

public enum DispatchRouteStatus
{
    Draft = 1,
    Scheduled = 2,
    Dispatched = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}

public enum RouteStopStatus
{
    Pending = 1,
    EnRoute = 2,
    Arrived = 3,
    Completed = 4,
    Failed = 5,
    Skipped = 6
}

public enum PodVerificationType
{
    Signature = 1,
    Photo = 2,
    PinCode = 3,
    Unverified = 4
}
