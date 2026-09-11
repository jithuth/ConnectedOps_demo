using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class MaintenancePlanRule : BaseEntity
{
    private MaintenancePlanRule()
    {
    }

    public MaintenancePlanRule(
        Guid tenantId,
        Guid maintenancePlanId,
        Guid maintenanceServiceTypeId,
        MaintenanceScheduleType scheduleType,
        decimal? intervalKilometers = null,
        decimal? intervalMiles = null,
        decimal? intervalEngineHours = null,
        int? intervalDays = null,
        int? intervalMonths = null,
        decimal? initialDueKilometers = null,
        decimal? initialDueEngineHours = null,
        DateTime? initialDueDateUtc = null,
        decimal? reminderBeforeKilometers = null,
        decimal? reminderBeforeEngineHours = null,
        int? reminderBeforeDays = null,
        decimal? toleranceKilometers = null,
        decimal? toleranceHours = null,
        int? toleranceDays = null,
        bool isMandatory = true,
        bool isActive = true,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenancePlanId == Guid.Empty)
            throw new ArgumentException("MaintenancePlanId is required.", nameof(maintenancePlanId));
        if (maintenanceServiceTypeId == Guid.Empty)
            throw new ArgumentException("MaintenanceServiceTypeId is required.", nameof(maintenanceServiceTypeId));

        TenantId = tenantId;
        MaintenancePlanId = maintenancePlanId;
        MaintenanceServiceTypeId = maintenanceServiceTypeId;
        ScheduleType = scheduleType;
        IntervalKilometers = intervalKilometers;
        IntervalMiles = intervalMiles;
        IntervalEngineHours = intervalEngineHours;
        IntervalDays = intervalDays;
        IntervalMonths = intervalMonths;
        InitialDueKilometers = initialDueKilometers;
        InitialDueEngineHours = initialDueEngineHours;
        InitialDueDateUtc = initialDueDateUtc;
        ReminderBeforeKilometers = reminderBeforeKilometers;
        ReminderBeforeEngineHours = reminderBeforeEngineHours;
        ReminderBeforeDays = reminderBeforeDays;
        ToleranceKilometers = toleranceKilometers;
        ToleranceHours = toleranceHours;
        ToleranceDays = toleranceDays;
        IsMandatory = isMandatory;
        IsActive = isActive;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenancePlanId { get; private set; }
    public MaintenancePlan MaintenancePlan { get; private set; } = null!;

    public Guid MaintenanceServiceTypeId { get; private set; }
    public MaintenanceServiceType MaintenanceServiceType { get; private set; } = null!;

    public MaintenanceScheduleType ScheduleType { get; private set; }

    public decimal? IntervalKilometers { get; private set; }
    public decimal? IntervalMiles { get; private set; }
    public decimal? IntervalEngineHours { get; private set; }
    public int? IntervalDays { get; private set; }
    public int? IntervalMonths { get; private set; }

    public decimal? InitialDueKilometers { get; private set; }
    public decimal? InitialDueEngineHours { get; private set; }
    public DateTime? InitialDueDateUtc { get; private set; }

    public decimal? ReminderBeforeKilometers { get; private set; }
    public decimal? ReminderBeforeEngineHours { get; private set; }
    public int? ReminderBeforeDays { get; private set; }

    public decimal? ToleranceKilometers { get; private set; }
    public decimal? ToleranceHours { get; private set; }
    public int? ToleranceDays { get; private set; }

    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        Guid maintenanceServiceTypeId,
        MaintenanceScheduleType scheduleType,
        decimal? intervalKilometers,
        decimal? intervalMiles,
        decimal? intervalEngineHours,
        int? intervalDays,
        int? intervalMonths,
        decimal? initialDueKilometers,
        decimal? initialDueEngineHours,
        DateTime? initialDueDateUtc,
        decimal? reminderBeforeKilometers,
        decimal? reminderBeforeEngineHours,
        int? reminderBeforeDays,
        decimal? toleranceKilometers,
        decimal? toleranceHours,
        int? toleranceDays,
        bool isMandatory,
        bool isActive,
        string? notes,
        Guid? updatedBy = null)
    {
        if (maintenanceServiceTypeId == Guid.Empty)
            throw new ArgumentException("MaintenanceServiceTypeId is required.", nameof(maintenanceServiceTypeId));

        MaintenanceServiceTypeId = maintenanceServiceTypeId;
        ScheduleType = scheduleType;
        IntervalKilometers = intervalKilometers;
        IntervalMiles = intervalMiles;
        IntervalEngineHours = intervalEngineHours;
        IntervalDays = intervalDays;
        IntervalMonths = intervalMonths;
        InitialDueKilometers = initialDueKilometers;
        InitialDueEngineHours = initialDueEngineHours;
        InitialDueDateUtc = initialDueDateUtc;
        ReminderBeforeKilometers = reminderBeforeKilometers;
        ReminderBeforeEngineHours = reminderBeforeEngineHours;
        ReminderBeforeDays = reminderBeforeDays;
        ToleranceKilometers = toleranceKilometers;
        ToleranceHours = toleranceHours;
        ToleranceDays = toleranceDays;
        IsMandatory = isMandatory;
        IsActive = isActive;
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
