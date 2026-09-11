using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverVehicleAssignment : BaseEntity
{
    private DriverVehicleAssignment()
    {
    }

    public DriverVehicleAssignment(
        Guid tenantId,
        Guid driverId,
        Guid vehicleId,
        AssignmentType assignmentType,
        DateTime assignedFromUtc,
        DateTime? assignedToUtc = null,
        bool isPrimary = true,
        Guid? assignedByUserId = null,
        string? reason = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        if (assignedToUtc.HasValue && assignedToUtc.Value <= assignedFromUtc)
            throw new ArgumentException("Assignment end time must be later than start time.");

        TenantId = tenantId;
        DriverId = driverId;
        VehicleId = vehicleId;
        AssignmentType = assignmentType;
        AssignedFromUtc = assignedFromUtc;
        AssignedToUtc = assignedToUtc;
        IsPrimary = isPrimary;
        IsActive = true;
        AssignedByUserId = assignedByUserId;
        Reason = reason?.Trim();
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public AssignmentType AssignmentType { get; private set; }
    public DateTime AssignedFromUtc { get; private set; }
    public DateTime? AssignedToUtc { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public Guid? EndedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public string? Notes { get; private set; }

    public void EndAssignment(Guid? endedByUserId, string? reason, DateTime? endedAtUtc = null)
    {
        var end = endedAtUtc ?? DateTime.UtcNow;
        if (end < AssignedFromUtc)
        {
            end = AssignedFromUtc;
        }

        AssignedToUtc = end;
        IsActive = false;
        EndedByUserId = endedByUserId;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Reason = string.IsNullOrWhiteSpace(Reason) ? reason.Trim() : $"{Reason} | Ended: {reason.Trim()}";
        }
        MarkUpdated();
    }

    public bool IsCurrentlyActive()
    {
        if (!IsActive) return false;
        var now = DateTime.UtcNow;
        return AssignedFromUtc <= now && (!AssignedToUtc.HasValue || AssignedToUtc.Value > now);
    }
}
