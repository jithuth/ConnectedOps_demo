using ConnectedOps.Domain.Maintenance;

namespace ConnectedOps.Application.Maintenance;

public sealed record MaintenancePlanDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? Description,
    Guid? VehicleCategoryId,
    string? VehicleCategoryName,
    bool IsActive,
    int RuleCount,
    int AssignedVehicleCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyCollection<MaintenancePlanRuleDto> Rules);

public sealed record MaintenancePlanListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    Guid? VehicleCategoryId,
    string? VehicleCategoryName,
    bool IsActive,
    int RuleCount,
    int AssignedVehicleCount,
    DateTime CreatedAtUtc);

public sealed record MaintenancePlanRuleDto(
    Guid Id,
    Guid MaintenancePlanId,
    Guid MaintenanceServiceTypeId,
    string ServiceTypeCode,
    string ServiceTypeName,
    MaintenanceServiceCategory ServiceCategory,
    string ServiceCategoryName,
    MaintenanceScheduleType ScheduleType,
    string ScheduleTypeName,
    decimal? IntervalKilometers,
    decimal? IntervalMiles,
    decimal? IntervalEngineHours,
    int? IntervalDays,
    int? IntervalMonths,
    decimal? InitialDueKilometers,
    decimal? InitialDueEngineHours,
    DateTime? InitialDueDateUtc,
    decimal? ReminderBeforeKilometers,
    decimal? ReminderBeforeEngineHours,
    int? ReminderBeforeDays,
    decimal? ToleranceKilometers,
    decimal? ToleranceHours,
    int? ToleranceDays,
    bool IsMandatory,
    bool IsActive,
    string? Notes);

public sealed record VehicleMaintenancePlanAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid MaintenancePlanId,
    string PlanCode,
    string PlanName,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    decimal? BaselineOdometer,
    decimal? BaselineEngineHours,
    bool IsActive,
    Guid? AssignedByUserId,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateMaintenancePlanRequest(
    string Code,
    string Name,
    string? Description = null,
    Guid? VehicleCategoryId = null,
    bool IsActive = true,
    IReadOnlyCollection<CreateMaintenancePlanRuleRequest>? Rules = null);

public sealed record UpdateMaintenancePlanRequest(
    string Code,
    string Name,
    string? Description = null,
    Guid? VehicleCategoryId = null,
    bool IsActive = true);

public sealed record CreateMaintenancePlanRuleRequest(
    Guid MaintenanceServiceTypeId,
    MaintenanceScheduleType ScheduleType,
    decimal? IntervalKilometers = null,
    decimal? IntervalMiles = null,
    decimal? IntervalEngineHours = null,
    int? IntervalDays = null,
    int? IntervalMonths = null,
    decimal? InitialDueKilometers = null,
    decimal? InitialDueEngineHours = null,
    DateTime? InitialDueDateUtc = null,
    decimal? ReminderBeforeKilometers = null,
    decimal? ReminderBeforeEngineHours = null,
    int? ReminderBeforeDays = null,
    decimal? ToleranceKilometers = null,
    decimal? ToleranceHours = null,
    int? ToleranceDays = null,
    bool IsMandatory = true,
    bool IsActive = true,
    string? Notes = null);

public sealed record UpdateMaintenancePlanRuleRequest(
    Guid MaintenanceServiceTypeId,
    MaintenanceScheduleType ScheduleType,
    decimal? IntervalKilometers = null,
    decimal? IntervalMiles = null,
    decimal? IntervalEngineHours = null,
    int? IntervalDays = null,
    int? IntervalMonths = null,
    decimal? InitialDueKilometers = null,
    decimal? InitialDueEngineHours = null,
    DateTime? InitialDueDateUtc = null,
    decimal? ReminderBeforeKilometers = null,
    decimal? ReminderBeforeEngineHours = null,
    int? ReminderBeforeDays = null,
    decimal? ToleranceKilometers = null,
    decimal? ToleranceHours = null,
    int? ToleranceDays = null,
    bool IsMandatory = true,
    bool IsActive = true,
    string? Notes = null);

public sealed record AssignVehiclePlanRequest(
    Guid VehicleId,
    Guid MaintenancePlanId,
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc = null,
    decimal? BaselineOdometer = null,
    decimal? BaselineEngineHours = null,
    string? Notes = null);

public sealed record UpdateVehiclePlanAssignmentRequest(
    DateTime EffectiveFromUtc,
    DateTime? EffectiveToUtc,
    decimal? BaselineOdometer,
    decimal? BaselineEngineHours,
    string? Notes,
    bool IsActive);
