using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Dispatch;

public sealed class DispatchRoute : BaseEntity
{
    private readonly List<DispatchRouteStop> _stops = [];

    private DispatchRoute()
    {
    }

    public DispatchRoute(
        Guid tenantId,
        string routeNumber,
        string name,
        DateOnly scheduledDate,
        Guid? vehicleId = null,
        Guid? driverId = null,
        Guid? startLocationId = null,
        Guid? endLocationId = null,
        decimal estimatedDistanceKm = 0m,
        int estimatedDurationMinutes = 0,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(routeNumber))
            throw new ArgumentException("RouteNumber is required.", nameof(routeNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        RouteNumber = routeNumber.Trim().ToUpperInvariant();
        Name = name.Trim();
        ScheduledDate = scheduledDate;
        VehicleId = vehicleId;
        DriverId = driverId;
        StartLocationId = startLocationId;
        EndLocationId = endLocationId;
        EstimatedDistanceKm = estimatedDistanceKm >= 0 ? estimatedDistanceKm : 0m;
        EstimatedDurationMinutes = estimatedDurationMinutes >= 0 ? estimatedDurationMinutes : 0;
        Status = DispatchRouteStatus.Draft;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string RouteNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateOnly ScheduledDate { get; private set; }
    public DispatchRouteStatus Status { get; private set; }

    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }

    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }

    public Guid? StartLocationId { get; private set; }
    public Location? StartLocation { get; private set; }

    public Guid? EndLocationId { get; private set; }
    public Location? EndLocation { get; private set; }

    public decimal EstimatedDistanceKm { get; private set; }
    public decimal? ActualDistanceKm { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public int? ActualDurationMinutes { get; private set; }

    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<DispatchRouteStop> Stops => _stops.OrderBy(s => s.SequenceOrder).ToList();

    public void AssignVehicleAndDriver(Guid? vehicleId, Guid? driverId, Guid? updatedBy = null)
    {
        VehicleId = vehicleId;
        DriverId = driverId;
        if (Status == DispatchRouteStatus.Draft && (vehicleId.HasValue || driverId.HasValue))
        {
            Status = DispatchRouteStatus.Scheduled;
        }
        MarkUpdated(updatedBy);
    }

    public void UpdateEstimates(decimal estimatedDistanceKm, int estimatedDurationMinutes, Guid? updatedBy = null)
    {
        EstimatedDistanceKm = estimatedDistanceKm;
        EstimatedDurationMinutes = estimatedDurationMinutes;
        MarkUpdated(updatedBy);
    }

    public void Dispatch(Guid? updatedBy = null)
    {
        if (Status == DispatchRouteStatus.Completed || Status == DispatchRouteStatus.Cancelled)
            throw new InvalidOperationException($"Cannot dispatch a route with status '{Status}'.");

        Status = DispatchRouteStatus.Dispatched;
        MarkUpdated(updatedBy);
    }

    public void StartRoute(DateTime? startedAtUtc = null, Guid? updatedBy = null)
    {
        if (Status == DispatchRouteStatus.Completed || Status == DispatchRouteStatus.Cancelled)
            throw new InvalidOperationException($"Cannot start a route with status '{Status}'.");

        Status = DispatchRouteStatus.InProgress;
        StartedAtUtc = startedAtUtc ?? DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void CompleteRoute(decimal? actualDistanceKm = null, DateTime? completedAtUtc = null, Guid? updatedBy = null)
    {
        Status = DispatchRouteStatus.Completed;
        CompletedAtUtc = completedAtUtc ?? DateTime.UtcNow;
        ActualDistanceKm = actualDistanceKm;
        if (StartedAtUtc.HasValue)
        {
            ActualDurationMinutes = (int)Math.Max(0, (CompletedAtUtc.Value - StartedAtUtc.Value).TotalMinutes);
        }
        MarkUpdated(updatedBy);
    }

    public void Cancel(string? reason = null, Guid? updatedBy = null)
    {
        Status = DispatchRouteStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Cancelled: {reason.Trim()}" : $"{Notes}\nCancelled: {reason.Trim()}";
        }
        MarkUpdated(updatedBy);
    }

    public void AddStop(DispatchRouteStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        if (!_stops.Any(s => s.Id == stop.Id))
        {
            _stops.Add(stop);
        }
    }

    public void RemoveStop(Guid stopId)
    {
        var stop = _stops.FirstOrDefault(s => s.Id == stopId);
        if (stop != null)
        {
            _stops.Remove(stop);
            // Re-index remaining stops
            var ordered = _stops.OrderBy(s => s.SequenceOrder).ToList();
            for (int i = 0; i < ordered.Count; i++)
            {
                ordered[i].UpdateSequence(i + 1);
            }
        }
    }

    public void UpdateGeneral(
        string name,
        DateOnly scheduledDate,
        Guid? vehicleId,
        Guid? driverId,
        Guid? startLocationId,
        Guid? endLocationId,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Name = name.Trim();
        ScheduledDate = scheduledDate;
        VehicleId = vehicleId;
        DriverId = driverId;
        StartLocationId = startLocationId;
        EndLocationId = endLocationId;
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
