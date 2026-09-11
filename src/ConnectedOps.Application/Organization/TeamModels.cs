namespace ConnectedOps.Application.Organization;

public sealed record TeamListItemDto(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    string Name,
    string Code,
    Guid? TeamLeadEmployeeId,
    string? TeamLeadName,
    int EmployeeCount,
    bool IsActive);

public sealed record TeamDto(
    Guid Id,
    Guid TenantId,
    Guid DepartmentId,
    string DepartmentName,
    string Name,
    string Code,
    string? Description,
    Guid? TeamLeadEmployeeId,
    string? TeamLeadName,
    int EmployeeCount,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateTeamRequest(
    Guid DepartmentId,
    string Name,
    string Code,
    string? Description,
    Guid? TeamLeadEmployeeId);

public sealed record UpdateTeamRequest(
    Guid DepartmentId,
    string Name,
    string Code,
    string? Description,
    Guid? TeamLeadEmployeeId);
