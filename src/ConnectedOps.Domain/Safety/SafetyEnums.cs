namespace ConnectedOps.Domain.Safety;

public enum SafetyIncidentType
{
    VehicleAccident = 1,
    NearMiss = 2,
    UnsafeAct = 3,
    UnsafeCondition = 4,
    EquipmentIncident = 5,
    PropertyDamage = 6,
    TrafficViolation = 7,
    DriverBehavior = 8,
    Environmental = 9,
    Other = 99
}

public enum SafetyIncidentSeverity
{
    Low = 1,
    Moderate = 2,
    High = 3,
    Critical = 4
}

public enum SafetyIncidentStatus
{
    Open = 1,
    UnderReview = 2,
    UnderInvestigation = 3,
    CorrectiveActionPending = 4,
    Closed = 5,
    Cancelled = 6
}

public enum SafetyParticipantType
{
    Driver = 1,
    Employee = 2,
    ThirdParty = 3,
    Other = 99
}

public enum SafetyEvidenceType
{
    Photo = 1,
    Video = 2,
    Document = 3,
    Statement = 4,
    TelematicsSnapshot = 5,
    Other = 99
}

public enum SafetyInvestigationStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum SafetyRootCause
{
    HumanFactor = 1,
    VehicleCondition = 2,
    EquipmentFailure = 3,
    RoadCondition = 4,
    Weather = 5,
    ProcedureFailure = 6,
    TrainingGap = 7,
    ThirdParty = 8,
    Unknown = 9,
    Other = 99
}

public enum SafetyViolationType
{
    Speeding = 1,
    HarshDriving = 2,
    UnauthorizedVehicleUse = 3,
    Seatbelt = 4,
    LicenseViolation = 5,
    InspectionViolation = 6,
    OverdueCompliance = 7,
    UnsafeEquipmentUse = 8,
    Other = 99
}

public enum SafetyViolationSource
{
    Manual = 1,
    Telematics = 2,
    Inspection = 3,
    Compliance = 4,
    External = 5,
    Other = 99
}

public enum CorrectiveActionStatus
{
    Open = 1,
    InProgress = 2,
    Completed = 3,
    Verified = 4,
    Cancelled = 5,
    Overdue = 6
}

public enum CorrectiveActionPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum SafetyRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum SafetyRiskLikelihood
{
    Rare = 1,
    Unlikely = 2,
    Possible = 3,
    Likely = 4,
    AlmostCertain = 5
}

public enum SafetyRiskImpact
{
    Minor = 1,
    Moderate = 2,
    Major = 3,
    Severe = 4,
    Catastrophic = 5
}
