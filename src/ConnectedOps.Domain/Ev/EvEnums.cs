namespace ConnectedOps.Domain.Ev;

public enum ChargingConnectorType
{
    Ccs2 = 1,
    Type2Menno = 2,
    TeslaNacs = 3,
    Chademo = 4,
    GbT = 5
}

public enum EvChargingStatus
{
    Disconnected = 1,
    ConnectedIdle = 2,
    ChargingAc = 3,
    ChargingDcFast = 4,
    FullyCharged = 5,
    Fault = 6
}

public enum ChargingStationType
{
    DepotPrivate = 1,
    PublicCommercial = 2,
    DestinationPartner = 3
}

public enum BatteryHealthCondition
{
    Optimal = 1,
    Normal = 2,
    Degrading = 3,
    CriticalReplacementNeeded = 4
}
