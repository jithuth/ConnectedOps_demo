using ConnectedOps.Domain.Maintenance;

namespace ConnectedOps.Application.Maintenance;

public sealed record MaintenanceServiceTypeDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    MaintenanceServiceCategory Category,
    string CategoryName,
    string? Description,
    decimal? DefaultDurationHours,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateMaintenanceServiceTypeRequest(
    string Code,
    string Name,
    MaintenanceServiceCategory Category = MaintenanceServiceCategory.Preventive,
    string? Description = null,
    decimal? DefaultDurationHours = null,
    bool IsActive = true);

public sealed record UpdateMaintenanceServiceTypeRequest(
    string Code,
    string Name,
    MaintenanceServiceCategory Category,
    string? Description = null,
    decimal? DefaultDurationHours = null,
    bool IsActive = true);
