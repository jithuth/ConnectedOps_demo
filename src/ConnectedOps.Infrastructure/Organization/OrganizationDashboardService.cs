using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class OrganizationDashboardService : IOrganizationDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public OrganizationDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<OrganizationDashboardSummaryDto> GetDashboardSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var totalBranches = await _dbContext.Branches
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var activeBranches = await _dbContext.Branches
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        var totalLocations = await _dbContext.Locations
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var totalDepartments = await _dbContext.Departments
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var totalTeams = await _dbContext.Teams
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var totalEmployees = await _dbContext.Employees
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var activeEmployees = await _dbContext.Employees
            .CountAsync(x => x.TenantId == tenantId && x.EmploymentStatus == EmploymentStatus.Active, cancellationToken);

        var linkedUserEmployees = await _dbContext.Employees
            .CountAsync(x => x.TenantId == tenantId && x.UserId != null, cancellationToken);

        var empByTypeData = await _dbContext.Employees
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.EmploymentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var employeesByEmploymentType = empByTypeData
            .ToDictionary(x => x.Type.ToString(), x => x.Count);

        var empByBranchData = await _dbContext.Employees
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.Branch != null ? x.Branch.Name : "Unassigned")
            .Select(g => new { BranchName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var employeesByBranch = empByBranchData
            .ToDictionary(x => x.BranchName, x => x.Count);

        var empByDeptData = await _dbContext.Employees
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.Department != null ? x.Department.Name : "Unassigned")
            .Select(g => new { DeptName = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var employeesByDepartment = empByDeptData
            .ToDictionary(x => x.DeptName, x => x.Count);

        return new OrganizationDashboardSummaryDto(
            totalBranches,
            activeBranches,
            totalLocations,
            totalDepartments,
            totalTeams,
            totalEmployees,
            activeEmployees,
            linkedUserEmployees,
            employeesByEmploymentType,
            employeesByBranch,
            employeesByDepartment);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
