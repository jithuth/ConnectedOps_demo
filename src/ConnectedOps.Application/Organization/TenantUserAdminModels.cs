namespace ConnectedOps.Application.Organization;

public sealed record TenantUserAdminDto(
    Guid TenantUserId,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? PhoneNumber,
    bool IsActive,
    bool IsDefaultTenant,
    DateTime JoinedAtUtc,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<Guid> RoleIds,
    Guid? EmployeeId,
    string? EmployeeNumber);

public sealed record CreateTenantUserAdminRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string Password,
    IReadOnlyCollection<Guid> RoleIds,
    bool CreateLinkedEmployee,
    Guid? BranchId,
    Guid? DepartmentId,
    Guid? TeamId,
    string? JobTitle);

public sealed record UpdateTenantUserAdminRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber,
    IReadOnlyCollection<Guid> RoleIds,
    Guid? EmployeeId);
