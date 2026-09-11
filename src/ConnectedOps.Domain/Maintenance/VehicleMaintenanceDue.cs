using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceDue : BaseEntity
{
    private VehicleMaintenanceDue()
    {
    }

    public VehicleMaintenanceDue(
        Guid tenantId,
        Guid vehicleId,
        Guid maintenancePlanRuleId,
        Guid? lastMaintenanceRecordId = null,
        DateTime? nextDueDateUtc = null,
        decimal? nextDueOdometer = null,
        decimal? nextDueEngineHours = null,
        MaintenanceDueStatus dueStatus = MaintenanceDueStatus.NotDue,
        DateTime? calculatedAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (maintenancePlanRuleId == Guid.Empty)
            throw new ArgumentException("MaintenancePlanRuleId is required.", nameof(maintenancePlanRuleId));

        TenantId = tenantId;
        VehicleId = vehicleId;
        MaintenancePlanRuleId = maintenancePlanRuleId;
        LastMaintenanceRecordId = lastMaintenanceRecordId;
        NextDueDateUtc = nextDueDateUtc;
        NextDueOdometer = nextDueOdometer;
        NextDueEngineHours = nextDueEngineHours;
        DueStatus = dueStatus;
        CalculatedAtUtc = calculatedAtUtc ?? DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public Guid MaintenancePlanRuleId { get; private set; }
    public MaintenancePlanRule MaintenancePlanRule { get; private set; } = null!;

    public Guid? LastMaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord? LastMaintenanceRecord { get; private set; }

    public DateTime? NextDueDateUtc { get; private set; }
    public decimal? NextDueOdometer { get; private set; }
    public decimal? NextDueEngineHours { get; private set; }

    public MaintenanceDueStatus DueStatus { get; private set; }
    public DateTime CalculatedAtUtc { get; private set; }

    public void UpdateProjection(
        Guid? lastMaintenanceRecordId,
        DateTime? nextDueDateUtc,
        decimal? nextDueOdometer,
        decimal? nextDueEngineHours,
        MaintenanceDueStatus dueStatus,
        DateTime? calculatedAtUtc = null)
    {
        LastMaintenanceRecordId = lastMaintenanceRecordId;
        NextDueDateUtc = nextDueDateUtc;
        NextDueOdometer = nextDueOdometer;
        NextDueEngineHours = nextDueEngineHours;
        DueStatus = dueStatus;
        CalculatedAtUtc = calculatedAtUtc ?? DateTime.UtcNow;
        MarkUpdated();
    }
}
