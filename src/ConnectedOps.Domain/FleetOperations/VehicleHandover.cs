using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.FleetOperations;

public sealed class VehicleHandover : BaseEntity
{
    private VehicleHandover()
    {
    }

    public VehicleHandover(
        Guid tenantId,
        Guid vehicleId,
        Guid toDriverId,
        decimal odometer,
        OdometerUnit odometerUnit = OdometerUnit.Kilometers,
        Guid? fromDriverId = null,
        Guid? fromUsageSessionId = null,
        Guid? toUsageSessionId = null,
        DateTime? handoverAtUtc = null,
        Guid? locationId = null,
        VehicleCondition condition = VehicleCondition.Good,
        string? notes = null,
        bool acknowledgedByFromDriver = true,
        bool acknowledgedByToDriver = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (toDriverId == Guid.Empty)
            throw new ArgumentException("ToDriverId is required.", nameof(toDriverId));
        if (fromDriverId.HasValue && fromDriverId.Value == toDriverId)
            throw new ArgumentException("ToDriver cannot be the same as FromDriver.");
        if (odometer < 0)
            throw new ArgumentException("Odometer cannot be negative.", nameof(odometer));

        TenantId = tenantId;
        VehicleId = vehicleId;
        ToDriverId = toDriverId;
        FromDriverId = fromDriverId;
        FromUsageSessionId = fromUsageSessionId;
        ToUsageSessionId = toUsageSessionId;
        HandoverAtUtc = handoverAtUtc ?? DateTime.UtcNow;
        Odometer = odometer;
        OdometerUnit = odometerUnit;
        LocationId = locationId;
        Condition = condition;
        Notes = notes?.Trim();
        AcknowledgedByFromDriver = acknowledgedByFromDriver;
        AcknowledgedByToDriver = acknowledgedByToDriver;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;
    public Guid? FromDriverId { get; private set; }
    public Driver? FromDriver { get; private set; }
    public Guid ToDriverId { get; private set; }
    public Driver ToDriver { get; private set; } = null!;
    public Guid? FromUsageSessionId { get; private set; }
    public VehicleUsageSession? FromUsageSession { get; private set; }
    public Guid? ToUsageSessionId { get; private set; }
    public VehicleUsageSession? ToUsageSession { get; private set; }
    public DateTime HandoverAtUtc { get; private set; }
    public decimal Odometer { get; private set; }
    public OdometerUnit OdometerUnit { get; private set; }
    public Guid? LocationId { get; private set; }
    public VehicleCondition Condition { get; private set; }
    public string? Notes { get; private set; }
    public bool AcknowledgedByFromDriver { get; private set; }
    public bool AcknowledgedByToDriver { get; private set; }

    public void LinkSessions(Guid? fromSessionId, Guid? toSessionId)
    {
        FromUsageSessionId = fromSessionId;
        ToUsageSessionId = toSessionId;
        MarkUpdated();
    }
}
