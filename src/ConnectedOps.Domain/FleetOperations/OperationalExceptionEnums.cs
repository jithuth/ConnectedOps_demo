namespace ConnectedOps.Domain.FleetOperations;

public enum OperationalExceptionType
{
    LateReturn = 1,
    OdometerMismatch = 2,
    VehicleConditionIssue = 3,
    DriverUnavailable = 4,
    VehicleUnavailable = 5,
    AssignmentConflict = 6,
    UnauthorizedCheckout = 7,
    Other = 99
}

public enum OperationalExceptionSeverity
{
    Information = 1,
    Warning = 2,
    High = 3,
    Critical = 4
}

public enum OperationalExceptionStatus
{
    Open = 1,
    Resolved = 2,
    Dismissed = 3
}
