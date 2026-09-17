namespace ConnectedOps.Domain.WhiteLabel;

public enum DomainVerificationStatus
{
    PendingDns = 1,
    Verified = 2,
    Failed = 3
}

public enum AuditPackageType
{
    Soc2Type2 = 1,
    Iso27001 = 2,
    GdprPrivacy = 3,
    FullGovernancePack = 4
}
