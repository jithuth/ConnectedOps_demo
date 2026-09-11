namespace ConnectedOps.Domain.Assets;

public enum AssetOwnershipType
{
    CompanyOwned = 1,
    Leased = 2,
    Rented = 3,
    CustomerOwned = 4,
    ThirdParty = 5,
    EmployeeOwned = 6,
    Other = 99
}

public enum AssetStatus
{
    Draft = 1,
    Available = 2,
    Assigned = 3,
    CheckedOut = 4,
    InUse = 5,
    UnderInspection = 6,
    UnderMaintenance = 7,
    Damaged = 8,
    Lost = 9,
    Inactive = 10,
    Retired = 11,
    Disposed = 12
}

public enum AssetAssignmentType
{
    Permanent = 1,
    Temporary = 2,
    Project = 3,
    Shift = 4,
    Emergency = 5,
    Other = 99
}

public enum AssetTransferStatus
{
    Pending = 1,
    InTransit = 2,
    Completed = 3,
    Cancelled = 4
}

public enum AssetTransferType
{
    Branch = 1,
    Location = 2,
    Employee = 3,
    Vehicle = 4,
    Combined = 5,
    Other = 99
}

public enum AssetUsageSessionStatus
{
    Open = 1,
    Completed = 2,
    Cancelled = 3,
    Overdue = 4
}

public enum AssetCondition
{
    Excellent = 1,
    Good = 2,
    Fair = 3,
    NeedsAttention = 4,
    Damaged = 5,
    Unsafe = 6
}

public enum AssetInspectionStatus
{
    Draft = 1,
    Completed = 2,
    Cancelled = 3
}

public enum AssetInspectionResult
{
    Passed = 1,
    PassedWithObservation = 2,
    Failed = 3,
    NotApplicable = 99
}

public enum AssetInspectionType
{
    Routine = 1,
    Safety = 2,
    PreUse = 3,
    PostUse = 4,
    Compliance = 5,
    Annual = 6,
    Other = 99
}

public enum AssetDocumentType
{
    PurchaseInvoice = 1,
    Warranty = 2,
    InspectionCertificate = 3,
    CalibrationCertificate = 4,
    Manual = 5,
    Insurance = 6,
    SafetyCertificate = 7,
    Photo = 8,
    Other = 99
}

public enum AssetIdentifierType
{
    QRCode = 1,
    Barcode = 2,
    RFID = 3,
    NFC = 4,
    Other = 99
}

public enum AssetUtilizationStatus
{
    ActiveUse = 1,
    OccasionalUse = 2,
    Idle = 3,
    NeverUsed = 4
}
