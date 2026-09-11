using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Application.Organization;

public sealed record EmployeeListItemDto(
    Guid Id,
    string EmployeeNumber,
    string FullName,
    string Email,
    string? Phone,
    string? JobTitle,
    EmploymentType EmploymentType,
    string EmploymentTypeName,
    EmploymentStatus EmploymentStatus,
    string EmploymentStatusName,
    string? BranchName,
    string? DepartmentName,
    string? TeamName,
    string? ManagerName,
    bool IsLinkedToUser,
    bool IsActive);

public sealed record EmployeeDto(
    Guid Id,
    Guid TenantId,
    Guid? UserId,
    Guid? BranchId,
    string? BranchName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? TeamId,
    string? TeamName,
    Guid? ManagerEmployeeId,
    string? ManagerName,
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    string? JobTitle,
    EmploymentType EmploymentType,
    string EmploymentTypeName,
    EmploymentStatus EmploymentStatus,
    string EmploymentStatusName,
    DateOnly? HireDate,
    DateOnly? TerminationDate,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle,
    EmploymentType EmploymentType,
    EmploymentStatus EmploymentStatus,
    Guid? BranchId,
    Guid? DepartmentId,
    Guid? TeamId,
    Guid? ManagerEmployeeId,
    Guid? UserId,
    DateOnly? HireDate);

public sealed record UpdateEmployeeRequest(
    string EmployeeNumber,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? JobTitle,
    EmploymentType EmploymentType,
    EmploymentStatus EmploymentStatus,
    Guid? BranchId,
    Guid? DepartmentId,
    Guid? TeamId,
    Guid? ManagerEmployeeId,
    DateOnly? HireDate,
    DateOnly? TerminationDate);

public sealed record LinkEmployeeUserRequest(
    Guid UserId);
