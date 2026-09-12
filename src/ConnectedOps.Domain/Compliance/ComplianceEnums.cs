namespace ConnectedOps.Domain.Compliance;

public enum ComplianceSubjectType
{
    Vehicle = 1,
    Driver = 2,
    Asset = 3
}

public enum ComplianceRequirementType
{
    Registration = 1,
    Insurance = 2,
    License = 3,
    Inspection = 4,
    Certification = 5,
    Permit = 6,
    Calibration = 7,
    Training = 8,
    MedicalFitness = 9,
    SafetyCheck = 10,
    MaintenanceCompliance = 11,
    Other = 99
}

public enum ComplianceValidityType
{
    NoExpiry = 1,
    FixedDate = 2,
    Duration = 3,
    Recurring = 4
}

public enum ComplianceStatus
{
    NotRequired = 1,
    Missing = 2,
    PendingVerification = 3,
    Valid = 4,
    ExpiringSoon = 5,
    Expired = 6,
    Suspended = 7,
    Rejected = 8,
    NotApplicable = 9
}

public enum ComplianceExceptionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

public enum ComplianceScoreGrade
{
    A = 1,
    B = 2,
    C = 3,
    D = 4,
    Critical = 5
}
