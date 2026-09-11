using ConnectedOps.Domain.Maintenance;

namespace ConnectedOps.Application.Maintenance;

public sealed record MaintenanceProviderDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    MaintenanceProviderType ProviderType,
    string ProviderTypeName,
    string? ContactPerson,
    string? Phone,
    string? Email,
    string? Address,
    Guid? BranchId,
    string? BranchName,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateMaintenanceProviderRequest(
    string Code,
    string Name,
    MaintenanceProviderType ProviderType = MaintenanceProviderType.InternalWorkshop,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    Guid? BranchId = null,
    string? Notes = null,
    bool IsActive = true);

public sealed record UpdateMaintenanceProviderRequest(
    string Code,
    string Name,
    MaintenanceProviderType ProviderType,
    string? ContactPerson = null,
    string? Phone = null,
    string? Email = null,
    string? Address = null,
    Guid? BranchId = null,
    string? Notes = null,
    bool IsActive = true);
