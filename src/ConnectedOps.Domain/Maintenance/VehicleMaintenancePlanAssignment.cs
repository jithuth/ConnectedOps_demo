using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenancePlanAssignment : BaseEntity
{
    private VehicleMaintenancePlanAssignment()
    {
    }

    public VehicleMaintenancePlanAssignment(
        Guid tenantId,
        Guid vehicleId,
        Guid maintenancePlanId,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc = null,
        decimal? baselineOdometer = null,
        decimal? baselineEngineHours = null,
        Guid? assignedByUserId = null,
        string? notes = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (maintenancePlanId == Guid.Empty)
            throw new ArgumentException("MaintenancePlanId is required.", nameof(maintenancePlanId));
        if (effectiveToUtc.HasValue && effectiveToUtc.Value < effectiveFromUtc)
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.");

        TenantId = tenantId;
        VehicleId = vehicleId;
        MaintenancePlanId = maintenancePlanId;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        BaselineOdometer = baselineOdometer;
        BaselineEngineHours = baselineEngineHours;
        AssignedByUserId = assignedByUserId;
        Notes = notes?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid MaintenancePlanId { get; private set; }
    public MaintenancePlan MaintenancePlan { get; private set; } = null!;

    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }

    public decimal? BaselineOdometer { get; private set; }
    public decimal? BaselineEngineHours { get; private set; }

    public bool IsActive { get; private set; }
    public Guid? AssignedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public void Deactivate(DateTime? effectiveToUtc = null, Guid? updatedBy = null)
    {
        IsActive = false;
        EffectiveToUtc = effectiveToUtc ?? DateTime.UtcNow;
        MarkUpdated(updatedBy);
    }

    public void Update(
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc,
        decimal? baselineOdometer,
        decimal? baselineEngineHours,
        string? notes,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (effectiveToUtc.HasValue && effectiveToUtc.Value < effectiveFromUtc)
            throw new ArgumentException("EffectiveToUtc cannot be earlier than EffectiveFromUtc.");

        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        BaselineOdometer = baselineOdometer;
        BaselineEngineHours = baselineEngineHours;
        Notes = notes?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
