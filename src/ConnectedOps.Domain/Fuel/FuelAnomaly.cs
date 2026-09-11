using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelAnomaly : BaseEntity
{
    private FuelAnomaly()
    {
    }

    public FuelAnomaly(
        Guid tenantId,
        Guid fuelTransactionId,
        Guid vehicleId,
        FuelAnomalyType anomalyType,
        FuelAnomalySeverity severity,
        string description,
        DateTime? detectedAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (fuelTransactionId == Guid.Empty)
            throw new ArgumentException("FuelTransactionId is required.", nameof(fuelTransactionId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));

        TenantId = tenantId;
        FuelTransactionId = fuelTransactionId;
        VehicleId = vehicleId;
        AnomalyType = anomalyType;
        Severity = severity;
        Description = description.Trim();
        DetectedAtUtc = detectedAtUtc ?? DateTime.UtcNow;
        Status = FuelAnomalyStatus.Open;
    }

    public Guid TenantId { get; private set; }
    public Guid FuelTransactionId { get; private set; }
    public FuelTransaction FuelTransaction { get; private set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public FuelAnomalyType AnomalyType { get; private set; }
    public FuelAnomalySeverity Severity { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime DetectedAtUtc { get; private set; }

    public FuelAnomalyStatus Status { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }
    public string? ResolutionNotes { get; private set; }

    public void Resolve(string? resolutionNotes, Guid? resolvedByUserId = null)
    {
        Status = FuelAnomalyStatus.Resolved;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByUserId = resolvedByUserId;
        ResolutionNotes = resolutionNotes?.Trim();
        MarkUpdated(resolvedByUserId);
    }

    public void Dismiss(string? dismissalReason, Guid? dismissedByUserId = null)
    {
        Status = FuelAnomalyStatus.Dismissed;
        ResolvedAtUtc = DateTime.UtcNow;
        ResolvedByUserId = dismissedByUserId;
        ResolutionNotes = dismissalReason?.Trim();
        MarkUpdated(dismissedByUserId);
    }
}
