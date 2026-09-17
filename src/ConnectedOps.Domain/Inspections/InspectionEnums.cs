namespace ConnectedOps.Domain.Inspections;

public enum DvirType
{
    PreTrip = 1,
    PostTrip = 2,
    Interim = 3
}

public enum DvirStatus
{
    Passed = 1,
    DefectsReported = 2,
    Repaired = 3,
    CertifiedSafe = 4,
    VehicleGrounded = 5
}

public enum DefectSeverity
{
    Minor = 1,
    Major = 2,
    Critical = 3
}
