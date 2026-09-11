using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleOdometerEntry : BaseEntity
{
    private VehicleOdometerEntry()
    {
    }

    public VehicleOdometerEntry(
        Guid tenantId,
        Guid vehicleId,
        decimal reading,
        OdometerUnit unit,
        DateTime readingDateUtc,
        OdometerSource source = OdometerSource.Manual,
        string? notes = null,
        Guid? recordedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (reading < 0)
            throw new ArgumentException("Odometer reading cannot be negative.", nameof(reading));

        TenantId = tenantId;
        VehicleId = vehicleId;
        Reading = reading;
        Unit = unit;
        ReadingDateUtc = readingDateUtc;
        Source = source;
        Notes = notes?.Trim();
        RecordedByUserId = recordedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public decimal Reading { get; private set; }
    public OdometerUnit Unit { get; private set; }
    public DateTime ReadingDateUtc { get; private set; }
    public OdometerSource Source { get; private set; }
    public string? Notes { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
}
