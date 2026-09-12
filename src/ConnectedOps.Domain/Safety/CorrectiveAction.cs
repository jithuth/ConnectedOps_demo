using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Safety;

public sealed class CorrectiveAction : BaseEntity
{
    private CorrectiveAction()
    {
    }

    public CorrectiveAction(
        Guid tenantId,
        string title,
        string description,
        CorrectiveActionPriority priority = CorrectiveActionPriority.Medium,
        DateTime? dueDateUtc = null,
        Guid? assignedEmployeeId = null,
        Guid? safetyIncidentId = null,
        Guid? safetyViolationId = null,
        Guid? complianceRecordId = null,
        bool verificationRequired = false,
        CorrectiveActionStatus status = CorrectiveActionStatus.Open,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        DueDateUtc = dueDateUtc;
        AssignedEmployeeId = assignedEmployeeId;
        SafetyIncidentId = safetyIncidentId;
        SafetyViolationId = safetyViolationId;
        ComplianceRecordId = complianceRecordId;
        VerificationRequired = verificationRequired;
        Status = status;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public CorrectiveActionPriority Priority { get; private set; }
    public DateTime? DueDateUtc { get; private set; }

    public Guid? AssignedEmployeeId { get; private set; }
    public Employee? AssignedEmployee { get; private set; }

    public Guid? SafetyIncidentId { get; private set; }
    public SafetyIncident? SafetyIncident { get; private set; }

    public Guid? SafetyViolationId { get; private set; }
    public SafetyViolation? SafetyViolation { get; private set; }

    public Guid? ComplianceRecordId { get; private set; }
    public ComplianceRecord? ComplianceRecord { get; private set; }

    public CorrectiveActionStatus Status { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }

    public bool VerificationRequired { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedByUserId { get; private set; }

    public string? ResolutionNotes { get; private set; }

    public void Update(
        string title,
        string description,
        CorrectiveActionPriority priority,
        DateTime? dueDateUtc,
        Guid? assignedEmployeeId,
        bool verificationRequired,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        DueDateUtc = dueDateUtc;
        AssignedEmployeeId = assignedEmployeeId;
        VerificationRequired = verificationRequired;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void Start(Guid? updatedByUserId = null)
    {
        if (Status == CorrectiveActionStatus.Open || Status == CorrectiveActionStatus.Overdue)
        {
            Status = CorrectiveActionStatus.InProgress;
            UpdatedBy = updatedByUserId;
            MarkUpdated();
        }
    }

    public void Complete(Guid completedByUserId, string? resolutionNotes = null, DateTime? completedAtUtc = null)
    {
        if (completedByUserId == Guid.Empty)
            throw new ArgumentException("CompletedByUserId is required.", nameof(completedByUserId));

        Status = CorrectiveActionStatus.Completed;
        CompletedByUserId = completedByUserId;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        ResolutionNotes = resolutionNotes?.Trim();
        UpdatedBy = completedByUserId;
        MarkUpdated();
    }

    public void Verify(Guid verifiedByUserId, DateTime? verifiedAtUtc = null)
    {
        if (verifiedByUserId == Guid.Empty)
            throw new ArgumentException("VerifiedByUserId is required.", nameof(verifiedByUserId));

        Status = CorrectiveActionStatus.Verified;
        VerifiedByUserId = verifiedByUserId;
        VerifiedAtUtc = verifiedAtUtc ?? DateTime.UtcNow;
        UpdatedBy = verifiedByUserId;
        MarkUpdated();
    }

    public void Cancel(Guid cancelledByUserId)
    {
        Status = CorrectiveActionStatus.Cancelled;
        UpdatedBy = cancelledByUserId;
        MarkUpdated();
    }

    public void CheckOverdue(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        if (DueDateUtc.HasValue && DueDateUtc.Value < now &&
            Status is CorrectiveActionStatus.Open or CorrectiveActionStatus.InProgress)
        {
            Status = CorrectiveActionStatus.Overdue;
            MarkUpdated();
        }
    }
}
