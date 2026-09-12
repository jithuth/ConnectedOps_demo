using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncident : BaseEntity
{
    private readonly List<SafetyIncidentParticipant> _participants = [];
    private readonly List<SafetyIncidentVehicle> _vehicles = [];
    private readonly List<SafetyIncidentAsset> _assets = [];
    private readonly List<SafetyIncidentEvidence> _evidence = [];
    private readonly List<CorrectiveAction> _correctiveActions = [];
    private readonly List<SafetyViolation> _violations = [];

    private SafetyIncident()
    {
    }

    public SafetyIncident(
        Guid tenantId,
        string incidentNumber,
        SafetyIncidentType incidentType,
        SafetyIncidentSeverity severity,
        DateTime occurredAtUtc,
        string title,
        string description,
        DateTime? reportedAtUtc = null,
        Guid? branchId = null,
        Guid? locationId = null,
        double? latitude = null,
        double? longitude = null,
        string? immediateActionTaken = null,
        Guid? reportedByUserId = null,
        bool investigationRequired = false,
        SafetyIncidentStatus status = SafetyIncidentStatus.Open,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(incidentNumber))
            throw new ArgumentException("Incident number is required.", nameof(incidentNumber));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        IncidentNumber = incidentNumber.Trim().ToUpperInvariant();
        IncidentType = incidentType;
        Severity = severity;
        OccurredAtUtc = occurredAtUtc;
        Title = title.Trim();
        Description = description.Trim();
        ReportedAtUtc = reportedAtUtc ?? DateTime.UtcNow;
        BranchId = branchId;
        LocationId = locationId;
        Latitude = latitude;
        Longitude = longitude;
        ImmediateActionTaken = immediateActionTaken?.Trim();
        ReportedByUserId = reportedByUserId;
        InvestigationRequired = investigationRequired;
        Status = status;
        CreatedBy = createdByUserId ?? reportedByUserId;
    }

    public Guid TenantId { get; private set; }
    public string IncidentNumber { get; private set; } = string.Empty;
    public SafetyIncidentType IncidentType { get; private set; }
    public SafetyIncidentSeverity Severity { get; private set; }
    public SafetyIncidentStatus Status { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }
    public DateTime ReportedAtUtc { get; private set; }

    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }

    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string? ImmediateActionTaken { get; private set; }

    public Guid? ReportedByUserId { get; private set; }
    public bool InvestigationRequired { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }

    public SafetyIncidentInvestigation? Investigation { get; private set; }

    public IReadOnlyCollection<SafetyIncidentParticipant> Participants => _participants;
    public IReadOnlyCollection<SafetyIncidentVehicle> Vehicles => _vehicles;
    public IReadOnlyCollection<SafetyIncidentAsset> Assets => _assets;
    public IReadOnlyCollection<SafetyIncidentEvidence> Evidence => _evidence;
    public IReadOnlyCollection<CorrectiveAction> CorrectiveActions => _correctiveActions;
    public IReadOnlyCollection<SafetyViolation> Violations => _violations;

    public void Update(
        SafetyIncidentType incidentType,
        SafetyIncidentSeverity severity,
        DateTime occurredAtUtc,
        string title,
        string description,
        Guid? branchId,
        Guid? locationId,
        double? latitude,
        double? longitude,
        string? immediateActionTaken,
        bool investigationRequired,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        IncidentType = incidentType;
        Severity = severity;
        OccurredAtUtc = occurredAtUtc;
        Title = title.Trim();
        Description = description.Trim();
        BranchId = branchId;
        LocationId = locationId;
        Latitude = latitude;
        Longitude = longitude;
        ImmediateActionTaken = immediateActionTaken?.Trim();
        InvestigationRequired = investigationRequired;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void SetStatus(SafetyIncidentStatus status, Guid? updatedByUserId = null)
    {
        Status = status;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void StartInvestigation(Guid? investigatorEmployeeId = null, Guid? startedByUserId = null)
    {
        InvestigationRequired = true;
        Status = SafetyIncidentStatus.UnderInvestigation;
        if (Investigation == null)
        {
            Investigation = new SafetyIncidentInvestigation(
                TenantId,
                Id,
                investigatorEmployeeId,
                DateTime.UtcNow,
                status: SafetyInvestigationStatus.InProgress,
                createdByUserId: startedByUserId);
        }
        else
        {
            Investigation.Start(investigatorEmployeeId, startedByUserId);
        }
        MarkUpdated();
    }

    public void SetInvestigation(SafetyIncidentInvestigation investigation)
    {
        ArgumentNullException.ThrowIfNull(investigation);
        Investigation = investigation;
        InvestigationRequired = true;
    }

    public void Close(Guid closedByUserId, DateTime? closedAtUtc = null)
    {
        if (closedByUserId == Guid.Empty)
            throw new ArgumentException("ClosedByUserId is required.", nameof(closedByUserId));

        Status = SafetyIncidentStatus.Closed;
        ClosedByUserId = closedByUserId;
        ClosedAtUtc = closedAtUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancel(Guid cancelledByUserId)
    {
        Status = SafetyIncidentStatus.Cancelled;
        UpdatedBy = cancelledByUserId;
        MarkUpdated();
    }

    public void AddParticipant(SafetyIncidentParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (participant.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and participant.");
        _participants.Add(participant);
    }

    public void AddVehicle(SafetyIncidentVehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        if (vehicle.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and vehicle.");
        _vehicles.Add(vehicle);
    }

    public void AddAsset(SafetyIncidentAsset asset)
    {
        ArgumentNullException.ThrowIfNull(asset);
        if (asset.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and asset.");
        _assets.Add(asset);
    }

    public void AddEvidence(SafetyIncidentEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and evidence.");
        _evidence.Add(evidence);
    }

    public void AddCorrectiveAction(CorrectiveAction action)
    {
        ArgumentNullException.ThrowIfNull(action);
        if (action.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and corrective action.");
        _correctiveActions.Add(action);
    }

    public void AddViolation(SafetyViolation violation)
    {
        ArgumentNullException.ThrowIfNull(violation);
        if (violation.TenantId != TenantId)
            throw new InvalidOperationException("Tenant mismatch between incident and violation.");
        _violations.Add(violation);
    }
}
