namespace ConnectedOps.Application.Organization;

public sealed record OrganizationSettingsDto(
    Guid Id,
    Guid TenantId,
    bool EnforceBranchAssignment,
    bool EnforceDepartmentAssignment,
    bool AutoCreateEmployeeForUser,
    int FiscalYearStartMonth,
    string? DefaultWorkingDaysJson,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpdateOrganizationSettingsRequest(
    bool EnforceBranchAssignment,
    bool EnforceDepartmentAssignment,
    bool AutoCreateEmployeeForUser,
    int FiscalYearStartMonth,
    string? DefaultWorkingDaysJson);
