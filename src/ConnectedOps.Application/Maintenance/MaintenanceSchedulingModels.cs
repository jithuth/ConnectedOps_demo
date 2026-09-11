using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Maintenance;

public static class MaintenanceUnitConverter
{
    private const decimal KmPerMile = 1.609344m;

    public static decimal ToKilometers(decimal value, OdometerUnit unit)
    {
        return unit switch
        {
            OdometerUnit.Miles => Math.Round(value * KmPerMile, 2),
            _ => value
        };
    }

    public static decimal FromKilometers(decimal kilometers, OdometerUnit targetUnit)
    {
        return targetUnit switch
        {
            OdometerUnit.Miles => Math.Round(kilometers / KmPerMile, 2),
            _ => kilometers
        };
    }
}

public sealed record VehicleMaintenanceDueDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string VehicleDisplayName,
    Guid? BranchId,
    string? BranchName,
    Guid VehicleCategoryId,
    string CategoryName,
    Guid MaintenancePlanId,
    string PlanCode,
    string PlanName,
    Guid MaintenancePlanRuleId,
    Guid ServiceTypeId,
    string ServiceTypeCode,
    string ServiceTypeName,
    MaintenanceServiceCategory ServiceCategory,
    string ServiceCategoryName,
    MaintenanceScheduleType ScheduleType,
    string ScheduleTypeName,
    DateTime? LastServiceDateUtc,
    decimal? LastServiceOdometer,
    decimal? LastServiceEngineHours,
    decimal? CurrentOdometer,
    decimal? CurrentEngineHours,
    OdometerUnit VehicleOdometerUnit,
    DateTime? NextDueDateUtc,
    decimal? NextDueOdometer,
    decimal? NextDueEngineHours,
    decimal? RemainingDistance,
    decimal? RemainingEngineHours,
    int? RemainingDays,
    MaintenanceDueStatus DueStatus,
    string DueStatusName,
    bool IsMandatory,
    DateTime CalculatedAtUtc);

public sealed record MaintenanceDueQueryParameters
{
    public MaintenanceDueStatus? Status { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? VehicleId { get; init; }
    public Guid? VehicleCategoryId { get; init; }
    public Guid? ServiceTypeId { get; init; }
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
