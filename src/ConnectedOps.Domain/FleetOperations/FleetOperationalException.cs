using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class FleetOperationalException : BaseEntity
{
    private FleetOperationalException()
    {
    }

    public FleetOperationalException(
        Guid tenantId,
        OperationalExceptionType exceptionType,
        OperationalExceptionSeverity severity,
        string description,
        Guid? vehicleId = null,
        Guid? driverId = null,
        Guid? usageSessionId = null,
        DateTime? occurredAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        ExceptionType = exceptionType;
        Severity = severity;
        Description = description.Trim();
        VehicleId = vehicleId;
        DriverId = driverId;
        UsageSessionId = usageSessionId;
        OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow;
        Status = OperationalExceptionStatus.Open;
    }

    public Guid TenantId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public Guid? UsageSessionId { get; private set; }
    public VehicleUsageSession? UsageSession { get; private set; }
    public OperationalExceptionType ExceptionType { get; private set; }
    public OperationalExceptionSeverity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime OccurredAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }
    public OperationalExceptionStatus Status { get; private set; }

    public void Resolve(Guid resolvedByUserId, string resolutionNotes)
    {
        if (Status != OperationalExceptionStatus.Open)
            throw new InvalidOperationException($"Cannot resolve exception with status '{Status}'. Only Open exceptions can be resolved.");

        if (string.IsNullOrWhiteSpace(resolutionNotes))
            throw new ArgumentException("Resolution notes are required when resolving an exception.", nameof(resolutionNotes));

        Status = OperationalExceptionStatus.Resolved;
        ResolvedByUserId = resolvedByUserId;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes.Trim();
        MarkUpdated();
    }

    public void Dismiss(Guid dismissedByUserId, string dismissalNotes)
    {
        if (Status != OperationalExceptionStatus.Open)
            throw new InvalidOperationException($"Cannot dismiss exception with status '{Status}'.");

        Status = OperationalExceptionStatus.Dismissed;
        ResolvedByUserId = dismissedByUserId;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolutionNotes = string.IsNullOrWhiteSpace(dismissalNotes) ? "Dismissed without notes" : dismissalNotes.Trim();
        MarkUpdated();
    }
}
