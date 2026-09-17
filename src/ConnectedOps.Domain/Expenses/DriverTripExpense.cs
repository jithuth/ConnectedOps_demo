using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Expenses;

public sealed class DriverTripExpense : BaseEntity
{
    private DriverTripExpense()
    {
    }

    public DriverTripExpense(
        Guid tenantId,
        Guid driverId,
        ExpenseCategory category,
        decimal amount,
        string description,
        DateTime incurredAtUtc,
        Guid? vehicleId = null,
        Guid? dispatchJobId = null,
        string currency = "AED",
        string? receiptImageKey = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        DriverId = driverId;
        Category = category;
        Amount = amount > 0 ? amount : 0m;
        Description = description.Trim();
        IncurredAtUtc = incurredAtUtc;
        VehicleId = vehicleId;
        DispatchJobId = dispatchJobId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "AED" : currency.Trim().ToUpperInvariant();
        ReceiptImageKey = receiptImageKey?.Trim();
        Status = ExpenseStatus.Submitted;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }

    public Guid DriverId { get; private set; }
    public Driver Driver { get; set; } = null!;

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; set; }

    public Guid? DispatchJobId { get; private set; }

    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "AED";
    public string Description { get; private set; } = string.Empty;
    public string? ReceiptImageKey { get; private set; }
    public DateTime IncurredAtUtc { get; private set; }

    public ExpenseStatus Status { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? ReimbursementReference { get; private set; }

    public void Approve(Guid approvedByUserId)
    {
        Status = ExpenseStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = DateTime.UtcNow;
        MarkUpdated(approvedByUserId);
    }

    public void Reject(Guid rejectedByUserId, string reason)
    {
        Status = ExpenseStatus.Rejected;
        ApprovedByUserId = rejectedByUserId;
        RejectionReason = reason.Trim();
        MarkUpdated(rejectedByUserId);
    }

    public void MarkReimbursed(string reference, Guid? updatedBy = null)
    {
        Status = ExpenseStatus.Reimbursed;
        ReimbursementReference = reference.Trim();
        MarkUpdated(updatedBy);
    }
}
