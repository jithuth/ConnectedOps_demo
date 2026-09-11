using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class FleetShiftAssignment : BaseEntity
{
    private FleetShiftAssignment()
    {
    }

    public FleetShiftAssignment(
        Guid tenantId,
        Guid fleetShiftId,
        DateOnly assignmentDate,
        DateTime startDateTimeUtc,
        DateTime? endDateTimeUtc = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        ShiftAssignmentStatus status = ShiftAssignmentStatus.Planned,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (fleetShiftId == Guid.Empty)
            throw new ArgumentException("FleetShiftId is required.", nameof(fleetShiftId));
        if (driverId == null && vehicleId == null)
            throw new ArgumentException("At least one of DriverId or VehicleId must be specified.");

        if (endDateTimeUtc.HasValue && endDateTimeUtc.Value <= startDateTimeUtc)
            throw new ArgumentException("End date time must be later than start date time.");

        TenantId = tenantId;
        FleetShiftId = fleetShiftId;
        AssignmentDate = assignmentDate;
        StartDateTimeUtc = startDateTimeUtc;
        EndDateTimeUtc = endDateTimeUtc;
        DriverId = driverId;
        VehicleId = vehicleId;
        AssignmentStatus = status;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FleetShiftId { get; private set; }
    public FleetShift FleetShift { get; private set; } = null!;
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Vehicle? Vehicle { get; private set; }
    public DateOnly AssignmentDate { get; private set; }
    public DateTime StartDateTimeUtc { get; private set; }
    public DateTime? EndDateTimeUtc { get; private set; }
    public ShiftAssignmentStatus AssignmentStatus { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateAssignment(
        Guid? driverId,
        Guid? vehicleId,
        DateTime startDateTimeUtc,
        DateTime? endDateTimeUtc,
        string? notes)
    {
        if (driverId == null && vehicleId == null)
            throw new ArgumentException("At least one of DriverId or VehicleId must be specified.");

        if (endDateTimeUtc.HasValue && endDateTimeUtc.Value <= startDateTimeUtc)
            throw new ArgumentException("End date time must be later than start date time.");

        DriverId = driverId;
        VehicleId = vehicleId;
        StartDateTimeUtc = startDateTimeUtc;
        EndDateTimeUtc = endDateTimeUtc;
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        AssignmentStatus = ShiftAssignmentStatus.Active;
        MarkUpdated();
    }

    public void Complete()
    {
        AssignmentStatus = ShiftAssignmentStatus.Completed;
        if (!EndDateTimeUtc.HasValue)
        {
            EndDateTimeUtc = DateTime.UtcNow;
        }
        MarkUpdated();
    }

    public void Cancel(string? reason)
    {
        AssignmentStatus = ShiftAssignmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? $"Cancelled: {reason.Trim()}" : $"{Notes} | Cancelled: {reason.Trim()}";
        }
        MarkUpdated();
    }
}
