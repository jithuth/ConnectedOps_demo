using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyViolation : BaseEntity
{
    private SafetyViolation()
    {
    }

    public SafetyViolation(
        Guid tenantId,
        SafetyViolationType violationType,
        SafetyIncidentSeverity severity,
        SafetyViolationSource source,
        DateTime occurredAtUtc,
        string description,
        Guid? driverId = null,
        Guid? employeeId = null,
        Guid? vehicleId = null,
        Guid? safetyIncidentId = null,
        string? reference = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        ViolationType = violationType;
        Severity = severity;
        Source = source;
        OccurredAtUtc = occurredAtUtc;
        Description = description.Trim();
        DriverId = driverId;
        EmployeeId = employeeId;
        VehicleId = vehicleId;
        SafetyIncidentId = safetyIncidentId;
        Reference = reference?.Trim();
        Notes = notes?.Trim();
        IsResolved = false;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public SafetyViolationType ViolationType { get; private set; }
    public SafetyIncidentSeverity Severity { get; private set; }
    public SafetyViolationSource Source { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string Description { get; private set; } = string.Empty;

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public Guid? SafetyIncidentId { get; private set; }
    public SafetyIncident? SafetyIncident { get; private set; }

    public string? Reference { get; private set; }
    public bool IsResolved { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        SafetyViolationType violationType,
        SafetyIncidentSeverity severity,
        SafetyViolationSource source,
        DateTime occurredAtUtc,
        string description,
        Guid? driverId,
        Guid? employeeId,
        Guid? vehicleId,
        Guid? safetyIncidentId,
        string? reference,
        string? notes,
        Guid? updatedByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        ViolationType = violationType;
        Severity = severity;
        Source = source;
        OccurredAtUtc = occurredAtUtc;
        Description = description.Trim();
        DriverId = driverId;
        EmployeeId = employeeId;
        VehicleId = vehicleId;
        SafetyIncidentId = safetyIncidentId;
        Reference = reference?.Trim();
        Notes = notes?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }

    public void Resolve(Guid resolvedByUserId, string? resolutionNotes = null, DateTime? resolvedAtUtc = null)
    {
        if (resolvedByUserId == Guid.Empty)
            throw new ArgumentException("ResolvedByUserId is required.", nameof(resolvedByUserId));

        IsResolved = true;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAtUtc = resolvedAtUtc ?? DateTime.UtcNow;
        ResolutionNotes = resolutionNotes?.Trim();
        UpdatedBy = resolvedByUserId;
        MarkUpdated();
    }

    public void Reopen(Guid updatedByUserId)
    {
        IsResolved = false;
        ResolvedByUserId = null;
        ResolvedAtUtc = null;
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }
}
