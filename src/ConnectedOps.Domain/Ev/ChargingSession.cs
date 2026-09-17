using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Ev;

public sealed class ChargingSession : BaseEntity
{
    private ChargingSession()
    {
    }

    public ChargingSession(
        Guid tenantId,
        Guid vehicleId,
        Guid chargingStationId,
        decimal startSocPercent,
        bool isScheduled = false,
        DateTime? scheduledStartUtc = null,
        string currency = "USD")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (chargingStationId == Guid.Empty)
            throw new ArgumentException("ChargingStationId is required.", nameof(chargingStationId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        ChargingStationId = chargingStationId;
        StartSocPercent = Math.Clamp(startSocPercent, 0m, 100m);
        IsScheduled = isScheduled;
        ScheduledStartUtc = scheduledStartUtc;
        StartedAtUtc = isScheduled ? (scheduledStartUtc ?? DateTime.UtcNow) : DateTime.UtcNow;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public Guid ChargingStationId { get; private set; }
    public ChargingStation ChargingStation { get; set; } = null!;

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public decimal StartSocPercent { get; private set; }
    public decimal? EndSocPercent { get; private set; }
    public decimal EnergyDeliveredKwh { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal Co2SavedKg { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; }
    public bool IsScheduled { get; private set; }
    public DateTime? ScheduledStartUtc { get; private set; }

    public void CompleteSession(decimal endSocPercent, decimal energyDeliveredKwh, decimal totalCost)
    {
        CompletedAtUtc = DateTime.UtcNow;
        EndSocPercent = Math.Clamp(endSocPercent, StartSocPercent, 100m);
        EnergyDeliveredKwh = Math.Max(0m, energyDeliveredKwh);
        TotalCost = Math.Max(0m, totalCost);
        Co2SavedKg = Math.Round(EnergyDeliveredKwh * 0.65m, 2);
        IsActive = false;
        MarkUpdated();
    }
}
