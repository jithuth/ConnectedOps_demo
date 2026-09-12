using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncidentInvestigation : BaseEntity
{
    private SafetyIncidentInvestigation()
    {
    }

    public SafetyIncidentInvestigation(
        Guid tenantId,
        Guid safetyIncidentId,
        Guid? investigatorEmployeeId = null,
        DateTime? startedAtUtc = null,
        DateTime? completedAtUtc = null,
        string? summary = null,
        SafetyRootCause rootCause = SafetyRootCause.Unknown,
        string? rootCauseDescription = null,
        string? contributingFactors = null,
        string? recommendation = null,
        SafetyInvestigationStatus status = SafetyInvestigationStatus.NotStarted,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (safetyIncidentId == Guid.Empty)
            throw new ArgumentException("SafetyIncidentId is required.", nameof(safetyIncidentId));

        TenantId = tenantId;
        SafetyIncidentId = safetyIncidentId;
        InvestigatorEmployeeId = investigatorEmployeeId;
        StartedAtUtc = startedAtUtc ?? DateTime.UtcNow;
        CompletedAtUtc = completedAtUtc;
        Summary = summary?.Trim();
        RootCause = rootCause;
        RootCauseDescription = rootCauseDescription?.Trim();
        ContributingFactors = contributingFactors?.Trim();
        Recommendation = recommendation?.Trim();
        Status = status;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid SafetyIncidentId { get; private set; }
    public SafetyIncident SafetyIncident { get; private set; } = null!;

    public Guid? InvestigatorEmployeeId { get; private set; }
    public Employee? InvestigatorEmployee { get; private set; }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    public string? Summary { get; private set; }
    public SafetyRootCause RootCause { get; private set; }
    public string? RootCauseDescription { get; private set; }
    public string? ContributingFactors { get; private set; }
    public string? Recommendation { get; private set; }
    public SafetyInvestigationStatus Status { get; private set; }

    public void Start(Guid? investigatorEmployeeId = null, Guid? startedByUserId = null)
    {
        InvestigatorEmployeeId = investigatorEmployeeId ?? InvestigatorEmployeeId;
        Status = SafetyInvestigationStatus.InProgress;
        UpdatedBy = startedByUserId;
        MarkUpdated();
    }

    public void UpdateFindings(
        Guid? investigatorEmployeeId,
        string? summary,
        SafetyRootCause rootCause,
        string? rootCauseDescription,
        string? contributingFactors,
        string? recommendation,
        Guid? updatedByUserId = null)
    {
        InvestigatorEmployeeId = investigatorEmployeeId;
        Summary = summary?.Trim();
        RootCause = rootCause;
        RootCauseDescription = rootCauseDescription?.Trim();
        ContributingFactors = contributingFactors?.Trim();
        Recommendation = recommendation?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void Complete(
        string summary,
        SafetyRootCause rootCause,
        string? rootCauseDescription,
        string? contributingFactors,
        string? recommendation,
        Guid completedByUserId,
        DateTime? completedAtUtc = null)
    {
        if (string.IsNullOrWhiteSpace(summary))
            throw new ArgumentException("Investigation summary is required for completion.", nameof(summary));

        Summary = summary.Trim();
        RootCause = rootCause;
        RootCauseDescription = rootCauseDescription?.Trim();
        ContributingFactors = contributingFactors?.Trim();
        Recommendation = recommendation?.Trim();
        Status = SafetyInvestigationStatus.Completed;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        UpdatedBy = completedByUserId;
        MarkUpdated();
    }

    public void Cancel(Guid cancelledByUserId)
    {
        Status = SafetyInvestigationStatus.Cancelled;
        UpdatedBy = cancelledByUserId;
        MarkUpdated();
    }
}
