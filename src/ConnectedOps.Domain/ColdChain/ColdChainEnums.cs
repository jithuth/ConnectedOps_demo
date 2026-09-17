namespace ConnectedOps.Domain.ColdChain;

public enum ReeferMode
{
    Off = 1,
    Cooling = 2,
    Freezing = 3,
    Defrost = 4,
    Standby = 5
}

public enum ExcursionSeverity
{
    Warning = 1,
    Critical = 2,
    HaccpBreach = 3
}

public enum ExcursionStatus
{
    Active = 1,
    Acknowledged = 2,
    Resolved = 3,
    CargoDiscarded = 4
}
