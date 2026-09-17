using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Ev;

public sealed class VehicleBatteryState : BaseEntity
{
    private VehicleBatteryState()
    {
    }

    public VehicleBatteryState(
        Guid tenantId,
        Guid vehicleId,
        decimal batteryCapacityKwh,
        decimal stateOfChargePercent = 100m,
        decimal stateOfHealthPercent = 100m,
        decimal remainingRangeKm = 350m,
        decimal batteryPackTempCelsius = 25m,
        int cycleCount = 0,
        int targetSocLimitPercent = 80,
        bool isOffPeakOnlyCharging = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (batteryCapacityKwh <= 0)
            throw new ArgumentException("BatteryCapacityKwh must be positive.", nameof(batteryCapacityKwh));

        TenantId = tenantId;
        VehicleId = vehicleId;
        BatteryCapacityKwh = batteryCapacityKwh;
        StateOfChargePercent = Math.Clamp(stateOfChargePercent, 0m, 100m);
        StateOfHealthPercent = Math.Clamp(stateOfHealthPercent, 0m, 100m);
        RemainingRangeKm = Math.Max(0m, remainingRangeKm);
        BatteryPackTempCelsius = batteryPackTempCelsius;
        CycleCount = Math.Max(0, cycleCount);
        TargetSocLimitPercent = Math.Clamp(targetSocLimitPercent, 50, 100);
        IsOffPeakOnlyCharging = isOffPeakOnlyCharging;
        ChargingStatus = EvChargingStatus.Disconnected;
        ActiveChargingPowerKw = 0m;
        HealthCondition = EvaluateCondition(stateOfHealthPercent);
        LastTelemetryAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; set; } = null!;

    public decimal BatteryCapacityKwh { get; private set; }
    public decimal StateOfChargePercent { get; private set; }
    public decimal StateOfHealthPercent { get; private set; }
    public decimal RemainingRangeKm { get; private set; }
    public decimal BatteryPackTempCelsius { get; private set; }
    public EvChargingStatus ChargingStatus { get; private set; }
    public decimal ActiveChargingPowerKw { get; private set; }
    public int CycleCount { get; private set; }
    public int TargetSocLimitPercent { get; private set; }
    public bool IsOffPeakOnlyCharging { get; private set; }
    public BatteryHealthCondition HealthCondition { get; private set; }
    public DateTime LastTelemetryAtUtc { get; private set; }

    public void UpdateTelemetry(
        decimal soc,
        decimal remainingRange,
        decimal packTemp,
        EvChargingStatus status,
        decimal chargingPowerKw)
    {
        StateOfChargePercent = Math.Clamp(soc, 0m, 100m);
        RemainingRangeKm = Math.Max(0m, remainingRange);
        BatteryPackTempCelsius = packTemp;
        ChargingStatus = status;
        ActiveChargingPowerKw = Math.Max(0m, chargingPowerKw);
        LastTelemetryAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void UpdateHealthProfile(decimal soh, int cycles)
    {
        StateOfHealthPercent = Math.Clamp(soh, 0m, 100m);
        CycleCount = Math.Max(0, cycles);
        HealthCondition = EvaluateCondition(StateOfHealthPercent);
        MarkUpdated();
    }

    public void ConfigureSmartCharging(int targetSoc, bool offPeakOnly)
    {
        TargetSocLimitPercent = Math.Clamp(targetSoc, 50, 100);
        IsOffPeakOnlyCharging = offPeakOnly;
        MarkUpdated();
    }

    private static BatteryHealthCondition EvaluateCondition(decimal soh)
    {
        if (soh >= 90m) return BatteryHealthCondition.Optimal;
        if (soh >= 80m) return BatteryHealthCondition.Normal;
        if (soh >= 70m) return BatteryHealthCondition.Degrading;
        return BatteryHealthCondition.CriticalReplacementNeeded;
    }
}
