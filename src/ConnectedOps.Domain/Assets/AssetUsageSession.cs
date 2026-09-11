using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetUsageSession : BaseEntity
{
    private AssetUsageSession()
    {
    }

    public AssetUsageSession(
        Guid tenantId,
        Guid assetId,
        Guid? employeeId = null,
        Guid? vehicleId = null,
        DateTime? checkedOutAtUtc = null,
        DateTime? expectedReturnAtUtc = null,
        AssetCondition conditionAtCheckout = AssetCondition.Good,
        string? purpose = null,
        string? reference = null,
        string? notes = null,
        Guid? checkedOutByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        EmployeeId = employeeId;
        VehicleId = vehicleId;
        CheckedOutAtUtc = checkedOutAtUtc ?? DateTime.UtcNow;
        ExpectedReturnAtUtc = expectedReturnAtUtc;
        ConditionAtCheckout = conditionAtCheckout;
        Purpose = purpose?.Trim();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
        Status = AssetUsageSessionStatus.Open;
        CheckedOutByUserId = checkedOutByUserId;
        CreatedBy = checkedOutByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public DateTime CheckedOutAtUtc { get; private set; }
    public Guid? CheckedOutByUserId { get; private set; }
    public DateTime? ExpectedReturnAtUtc { get; private set; }

    public DateTime? CheckedInAtUtc { get; private set; }
    public Guid? CheckedInByUserId { get; private set; }

    public AssetCondition ConditionAtCheckout { get; private set; }
    public AssetCondition? ConditionAtCheckin { get; private set; }

    public string? Purpose { get; private set; }
    public string? Reference { get; private set; }
    public AssetUsageSessionStatus Status { get; private set; }
    public string? Notes { get; private set; }

    public void CompleteCheckin(
        DateTime checkedInAtUtc,
        AssetCondition conditionAtCheckin,
        string? checkinNotes = null,
        Guid? checkedInByUserId = null)
    {
        if (Status != AssetUsageSessionStatus.Open && Status != AssetUsageSessionStatus.Overdue)
            throw new InvalidOperationException("Only open or overdue sessions can be checked in.");

        CheckedInAtUtc = checkedInAtUtc;
        ConditionAtCheckin = conditionAtCheckin;
        Status = AssetUsageSessionStatus.Completed;
        CheckedInByUserId = checkedInByUserId;
        if (!string.IsNullOrWhiteSpace(checkinNotes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? checkinNotes.Trim() : $"{Notes} | Check-in: {checkinNotes.Trim()}";
        }
        MarkUpdated(checkedInByUserId);
    }

    public void MarkOverdue()
    {
        if (Status == AssetUsageSessionStatus.Open)
        {
            Status = AssetUsageSessionStatus.Overdue;
            MarkUpdated();
        }
    }

    public void Cancel(string? cancellationReason = null, Guid? cancelledByUserId = null)
    {
        if (Status == AssetUsageSessionStatus.Completed)
            throw new InvalidOperationException("Completed session cannot be cancelled.");

        Status = AssetUsageSessionStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(cancellationReason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? cancellationReason.Trim() : $"{Notes} | Cancelled: {cancellationReason.Trim()}";
        }
        MarkUpdated(cancelledByUserId);
    }
}
