using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Geofences;

public sealed class VehicleGeofenceState : BaseEntity
{
    private VehicleGeofenceState()
    {
    }

    public VehicleGeofenceState(
        Guid tenantId,
        Guid vehicleId,
        Guid geofenceId,
        bool isInside,
        DateTime lastEvaluatedAtUtc,
        DateTime? lastEnteredAtUtc = null,
        DateTime? lastExitedAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (geofenceId == Guid.Empty)
            throw new ArgumentException("GeofenceId is required.", nameof(geofenceId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        GeofenceId = geofenceId;
        IsInside = isInside;
        LastEvaluatedAtUtc = lastEvaluatedAtUtc;
        LastEnteredAtUtc = lastEnteredAtUtc;
        LastExitedAtUtc = lastExitedAtUtc;
    }

    public Guid TenantId { get; private set; }

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid GeofenceId { get; private set; }
    public Geofence Geofence { get; private set; } = null!;

    public bool IsInside { get; private set; }
    public DateTime LastEvaluatedAtUtc { get; private set; }
    public DateTime? LastEnteredAtUtc { get; private set; }
    public DateTime? LastExitedAtUtc { get; private set; }

    public void UpdateState(bool isInside, DateTime evaluatedAtUtc)
    {
        LastEvaluatedAtUtc = evaluatedAtUtc;

        if (!IsInside && isInside)
        {
            IsInside = true;
            LastEnteredAtUtc = evaluatedAtUtc;
        }
        else if (IsInside && !isInside)
        {
            IsInside = false;
            LastExitedAtUtc = evaluatedAtUtc;
        }

        MarkUpdated();
    }
}
