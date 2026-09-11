namespace ConnectedOps.Application.Organization;

public sealed record DepartmentListItemDto(
    Guid Id,
    Guid? BranchId,
    string? BranchName,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    string Name,
    string Code,
    int TeamCount,
    int EmployeeCount,
    bool IsActive);

public sealed record DepartmentDto(
    Guid Id,
    Guid TenantId,
    Guid? BranchId,
    string? BranchName,
    Guid? ParentDepartmentId,
    string? ParentDepartmentName,
    string Name,
    string Code,
    string? Description,
    bool IsActive,
    int TeamCount,
    int EmployeeCount,
    int SubDepartmentCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateDepartmentRequest(
    Guid? BranchId,
    Guid? ParentDepartmentId,
    string Name,
    string Code,
    string? Description);

public sealed record UpdateDepartmentRequest(
    Guid? BranchId,
    Guid? ParentDepartmentId,
    string Name,
    string Code,
    string? Description);
