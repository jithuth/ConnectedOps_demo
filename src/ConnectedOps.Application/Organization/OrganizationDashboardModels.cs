namespace ConnectedOps.Application.Organization;

public sealed record OrganizationDashboardSummaryDto(
    int TotalBranches,
    int ActiveBranches,
    int TotalLocations,
    int TotalDepartments,
    int TotalTeams,
    int TotalEmployees,
    int ActiveEmployees,
    int LinkedUserEmployees,
    IReadOnlyDictionary<string, int> EmployeesByEmploymentType,
    IReadOnlyDictionary<string, int> EmployeesByBranch,
    IReadOnlyDictionary<string, int> EmployeesByDepartment);
