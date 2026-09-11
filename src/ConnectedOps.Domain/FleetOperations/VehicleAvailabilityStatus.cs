namespace ConnectedOps.Domain.FleetOperations;

public enum VehicleAvailabilityStatus
{
    Available = 1,
    Assigned = 2,
    CheckedOut = 3,
    Reserved = 4,
    Unavailable = 5,
    OutOfService = 6
}
