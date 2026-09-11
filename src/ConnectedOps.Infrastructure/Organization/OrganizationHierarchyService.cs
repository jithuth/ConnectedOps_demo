using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class OrganizationHierarchyService : IOrganizationHierarchyService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public OrganizationHierarchyService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<OrganizationHierarchyNodeDto>> GetHierarchyAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var tenant = await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new KeyNotFoundException("Tenant was not found.");

        var profile = await _dbContext.OrganizationProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        var orgName = profile?.LegalName ?? tenant.Name;

        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderByDescending(x => x.IsHeadOffice)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var locations = await _dbContext.Locations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var departments = await _dbContext.Departments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var teams = await _dbContext.Teams
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var employees = await _dbContext.Employees
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.EmploymentStatus == Domain.Organization.EmploymentStatus.Active)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);

        // Build hierarchy
        var branchNodes = new List<OrganizationHierarchyNodeDto>();

        foreach (var branch in branches)
        {
            var branchLocations = locations
                .Where(l => l.BranchId == branch.Id)
                .Select(l => new OrganizationHierarchyNodeDto(
                    l.Id,
                    l.Name,
                    "Location",
                    l.Code,
                    l.Type.ToString(),
                    Array.Empty<OrganizationHierarchyNodeDto>()))
                .ToList();

            var branchDepts = departments
                .Where(d => d.BranchId == branch.Id && !d.ParentDepartmentId.HasValue)
                .Select(d => BuildDepartmentNode(d, departments, teams, employees))
                .ToList();

            var unassignedEmployees = employees
                .Where(e => e.BranchId == branch.Id && !e.DepartmentId.HasValue && !e.TeamId.HasValue)
                .Select(e => new OrganizationHierarchyNodeDto(
                    e.Id,
                    $"{e.FirstName} {e.LastName}".Trim(),
                    "Employee",
                    e.EmployeeNumber,
                    e.JobTitle ?? "Staff",
                    Array.Empty<OrganizationHierarchyNodeDto>()))
                .ToList();

            var branchChildren = new List<OrganizationHierarchyNodeDto>();
            branchChildren.AddRange(branchLocations);
            branchChildren.AddRange(branchDepts);
            branchChildren.AddRange(unassignedEmployees);

            branchNodes.Add(new OrganizationHierarchyNodeDto(
                branch.Id,
                branch.Name + (branch.IsHeadOffice ? " (Head Office)" : string.Empty),
                "Branch",
                branch.Code,
                branch.City,
                branchChildren));
        }

        // Catch departments not tied to any branch
        var unassignedDepts = departments
            .Where(d => !d.BranchId.HasValue && !d.ParentDepartmentId.HasValue)
            .Select(d => BuildDepartmentNode(d, departments, teams, employees))
            .ToList();

        var rootChildren = new List<OrganizationHierarchyNodeDto>();
        rootChildren.AddRange(branchNodes);
        rootChildren.AddRange(unassignedDepts);

        var rootNode = new OrganizationHierarchyNodeDto(
            tenant.Id,
            orgName,
            "Organization",
            tenant.Code,
            profile?.TradeName,
            rootChildren);

        return [rootNode];
    }

    private static OrganizationHierarchyNodeDto BuildDepartmentNode(
        Department dept,
        List<Department> allDepts,
        List<Team> allTeams,
        List<Employee> allEmployees)
    {
        var children = new List<OrganizationHierarchyNodeDto>();

        // Sub departments
        var subDepts = allDepts
            .Where(d => d.ParentDepartmentId == dept.Id)
            .Select(d => BuildDepartmentNode(d, allDepts, allTeams, allEmployees));
        children.AddRange(subDepts);

        // Teams
        var deptTeams = allTeams
            .Where(t => t.DepartmentId == dept.Id)
            .Select(t =>
            {
                var teamEmployees = allEmployees
                    .Where(e => e.TeamId == t.Id)
                    .Select(e => new OrganizationHierarchyNodeDto(
                        e.Id,
                        $"{e.FirstName} {e.LastName}".Trim() + (e.Id == t.TeamLeadEmployeeId ? " (Lead)" : string.Empty),
                        "Employee",
                        e.EmployeeNumber,
                        e.JobTitle ?? "Team Member",
                        Array.Empty<OrganizationHierarchyNodeDto>()))
                    .ToList();

                return new OrganizationHierarchyNodeDto(
                    t.Id,
                    t.Name,
                    "Team",
                    t.Code,
                    $"Members: {teamEmployees.Count}",
                    teamEmployees);
            });
        children.AddRange(deptTeams);

        // Dept employees not in any team
        var deptDirectEmployees = allEmployees
            .Where(e => e.DepartmentId == dept.Id && !e.TeamId.HasValue)
            .Select(e => new OrganizationHierarchyNodeDto(
                e.Id,
                $"{e.FirstName} {e.LastName}".Trim(),
                "Employee",
                e.EmployeeNumber,
                e.JobTitle ?? "Staff",
                Array.Empty<OrganizationHierarchyNodeDto>()));
        children.AddRange(deptDirectEmployees);

        return new OrganizationHierarchyNodeDto(
            dept.Id,
            dept.Name,
            "Department",
            dept.Code,
            dept.Description,
            children);
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
