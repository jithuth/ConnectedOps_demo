using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Dispatch;

public sealed class DispatchRouteStop : BaseEntity
{
    private DispatchRouteStop()
    {
    }

    public DispatchRouteStop(
        Guid tenantId,
        Guid routeId,
        Guid jobId,
        int sequenceOrder,
        DateTime? plannedArrivalUtc = null,
        decimal estimatedDistanceKm = 0m,
        int estimatedMinutes = 0,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (routeId == Guid.Empty)
            throw new ArgumentException("RouteId is required.", nameof(routeId));
        if (jobId == Guid.Empty)
            throw new ArgumentException("JobId is required.", nameof(jobId));
        if (sequenceOrder <= 0)
            throw new ArgumentException("SequenceOrder must be greater than zero.", nameof(sequenceOrder));

        TenantId = tenantId;
        RouteId = routeId;
        JobId = jobId;
        SequenceOrder = sequenceOrder;
        PlannedArrivalUtc = plannedArrivalUtc;
        EstimatedDistanceKm = estimatedDistanceKm >= 0 ? estimatedDistanceKm : 0m;
        EstimatedMinutes = estimatedMinutes >= 0 ? estimatedMinutes : 0;
        Status = RouteStopStatus.Pending;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid RouteId { get; private set; }
    public DispatchRoute Route { get; set; } = null!;

    public Guid JobId { get; private set; }
    public DispatchJob Job { get; set; } = null!;

    public int SequenceOrder { get; private set; }
    public RouteStopStatus Status { get; private set; }
    public ProofOfDelivery? ProofOfDelivery { get; set; }

    public DateTime? PlannedArrivalUtc { get; private set; }
    public DateTime? ActualArrivalUtc { get; private set; }
    public DateTime? ActualDepartureUtc { get; private set; }

    public decimal EstimatedDistanceKm { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateSequence(int sequenceOrder, Guid? updatedBy = null)
    {
        if (sequenceOrder <= 0)
            throw new ArgumentException("SequenceOrder must be positive.", nameof(sequenceOrder));
        SequenceOrder = sequenceOrder;
        MarkUpdated(updatedBy);
    }

    public void UpdatePlannedArrival(DateTime? plannedArrivalUtc, Guid? updatedBy = null)
    {
        PlannedArrivalUtc = plannedArrivalUtc;
        MarkUpdated(updatedBy);
    }

    public void MarkEnRoute(Guid? updatedBy = null)
    {
        Status = RouteStopStatus.EnRoute;
        MarkUpdated(updatedBy);
    }

    public void MarkArrived(DateTime? arrivedAtUtc = null, Guid? updatedBy = null)
    {
        Status = RouteStopStatus.Arrived;
        ActualArrivalUtc = arrivedAtUtc ?? DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void MarkCompleted(DateTime? departedAtUtc = null, Guid? updatedBy = null)
    {
        Status = RouteStopStatus.Completed;
        ActualDepartureUtc = departedAtUtc ?? DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void MarkFailed(string reason, Guid? updatedBy = null)
    {
        Status = RouteStopStatus.Failed;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Failed: {reason.Trim()}" : $"{Notes}\nFailed: {reason.Trim()}";
        }
        MarkUpdated(updatedBy);
    }

    public void MarkSkipped(string? reason = null, Guid? updatedBy = null)
    {
        Status = RouteStopStatus.Skipped;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Skipped: {reason.Trim()}" : $"{Notes}\nSkipped: {reason.Trim()}";
        }
        MarkUpdated(updatedBy);
    }
}
