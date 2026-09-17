namespace ConnectedOps.Domain.TollsAndFines;

public enum TollSystemType
{
    Salik = 1,
    Darb = 2,
    Other = 3
}

public enum ViolationLiabilityStatus
{
    PendingReview = 1,
    AssignedToDriver = 2,
    CompanyPaid = 3,
    PayrollDeducted = 4,
    Disputed = 5,
    Dismissed = 6
}
