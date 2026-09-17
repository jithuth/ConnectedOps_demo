namespace ConnectedOps.Domain.Optimization;

public enum OptimizationObjective
{
    MinimizeDistance = 1,
    MinimizeDuration = 2,
    MinimizeCost = 3,
    BalanceWorkload = 4
}

public enum OptimizationRunStatus
{
    Pending = 1,
    Solving = 2,
    Completed = 3,
    Failed = 4,
    Dispatched = 5
}

public enum OptimizedStopType
{
    DepotStart = 1,
    Pickup = 2,
    Delivery = 3,
    DepotEnd = 4
}
