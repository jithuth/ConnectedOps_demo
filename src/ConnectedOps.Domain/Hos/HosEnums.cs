namespace ConnectedOps.Domain.Hos;

public enum DutyStatus
{
    OffDuty = 1,
    SleeperBerth = 2,
    Driving = 3,
    OnDutyNotDriving = 4,
    YardMove = 5,
    PersonalConveyance = 6
}

public enum HosPresetType
{
    GccUaeStandard = 1,
    EuTachograph = 2,
    UsFmcsa = 3,
    Custom = 4
}

public enum HosViolationType
{
    DrivingLimitExceeded = 1,
    ShiftWindowExceeded = 2,
    MissingRestBreak = 3,
    CycleLimitExceeded = 4
}
