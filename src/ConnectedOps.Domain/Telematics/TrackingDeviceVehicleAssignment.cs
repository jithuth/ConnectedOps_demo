using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Telematics;

public sealed class TrackingDeviceVehicleAssignment : BaseEntity
{
    private TrackingDeviceVehicleAssignment()
    {
    }

    public TrackingDeviceVehicleAssignment(
        Guid tenantId,
        Guid trackingDeviceId,
        Guid vehicleId,
        DateTime assignedFromUtc,
        bool isPrimary = true,
        Guid? assignedByUserId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (trackingDeviceId == Guid.Empty)
            throw new ArgumentException("TrackingDeviceId is required.", nameof(trackingDeviceId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        TrackingDeviceId = trackingDeviceId;
        VehicleId = vehicleId;
        AssignedFromUtc = assignedFromUtc;
        IsPrimary = isPrimary;
        IsActive = true;
        AssignedByUserId = assignedByUserId;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid TrackingDeviceId { get; private set; }
    public TrackingDevice TrackingDevice { get; private set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public DateTime AssignedFromUtc { get; private set; }
    public DateTime? AssignedToUtc { get; private set; }

    public bool IsPrimary { get; private set; } = true;
    public bool IsActive { get; private set; } = true;

    public Guid? AssignedByUserId { get; private set; }
    public Guid? EndedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public void EndAssignment(Guid? endedByUserId, DateTime? endedAtUtc = null, string? notes = null)
    {
        if (!IsActive)
            return;

        IsActive = false;
        EndedByUserId = endedByUserId;
        AssignedToUtc = endedAtUtc ?? DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes.Trim() : $"{Notes} | {notes.Trim()}";
        }
        MarkUpdated(endedByUserId);
    }
}
