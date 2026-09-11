using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class VehicleConditionRecord : BaseEntity
{
    private VehicleConditionRecord()
    {
    }

    public VehicleConditionRecord(
        Guid tenantId,
        Guid vehicleId,
        VehicleCondition condition,
        decimal? odometer = null,
        Guid? driverId = null,
        Guid? usageSessionId = null,
        Guid? recordedByUserId = null,
        DateTime? recordedAtUtc = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        Condition = condition;
        Odometer = odometer;
        DriverId = driverId;
        UsageSessionId = usageSessionId;
        RecordedByUserId = recordedByUserId;
        RecordedAtUtc = recordedAtUtc ?? DateTime.UtcNow;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public Guid? DriverId { get; private set; }
    public Driver? Driver { get; private set; }
    public Guid? UsageSessionId { get; private set; }
    public VehicleUsageSession? UsageSession { get; private set; }
    public VehicleCondition Condition { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public decimal? Odometer { get; private set; }
    public string? Notes { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
}
