using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.ColdChain;

public sealed class ColdChainExcursion : BaseEntity
{
    private ColdChainExcursion()
    {
    }

    public ColdChainExcursion(
        Guid tenantId,
        Guid cargoSensorDeviceId,
        Guid vehicleId,
        string compartmentName,
        double breachTemperatureCelsius,
        double allowableMinCelsius,
        double allowableMaxCelsius,
        ExcursionSeverity severity,
        DateTime startedAtUtc,
        string? locationName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (cargoSensorDeviceId == Guid.Empty)
            throw new ArgumentException("CargoSensorDeviceId is required.", nameof(cargoSensorDeviceId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));

        TenantId = tenantId;
        CargoSensorDeviceId = cargoSensorDeviceId;
        VehicleId = vehicleId;
        CompartmentName = compartmentName.Trim();
        BreachTemperatureCelsius = breachTemperatureCelsius;
        AllowableMinCelsius = allowableMinCelsius;
        AllowableMaxCelsius = allowableMaxCelsius;
        Severity = severity;
        StartedAtUtc = startedAtUtc;
        Status = ExcursionStatus.Active;
        LocationName = locationName?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid CargoSensorDeviceId { get; private set; }
    public CargoSensorDevice CargoSensorDevice { get; set; } = null!;

    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public string CompartmentName { get; private set; } = string.Empty;
    public double BreachTemperatureCelsius { get; private set; }
    public double AllowableMinCelsius { get; private set; }
    public double AllowableMaxCelsius { get; private set; }
    public ExcursionSeverity Severity { get; private set; }
    public ExcursionStatus Status { get; private set; }

    public DateTime StartedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }
    public int? DurationMinutes { get; private set; }
    public string? LocationName { get; private set; }
    public string? ActionTaken { get; private set; }
    public Guid? ResolvedByUserId { get; private set; }

    public void Resolve(string actionTaken, Guid userId)
    {
        ResolvedAtUtc = DateTime.UtcNow;
        DurationMinutes = (int)Math.Max(1, (ResolvedAtUtc.Value - StartedAtUtc).TotalMinutes);
        ActionTaken = actionTaken.Trim();
        ResolvedByUserId = userId;
        Status = ExcursionStatus.Resolved;
        MarkUpdated(userId);
    }
}
