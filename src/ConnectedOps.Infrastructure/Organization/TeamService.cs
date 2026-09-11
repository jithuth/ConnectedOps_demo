using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class TeamService : ITeamService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public TeamService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<TeamListItemDto>> GetTeamsAsync(
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.TeamLead)
            .Where(x => x.TenantId == tenantId);

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(x => x.DepartmentId == departmentId.Value);
        }

        var teams = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.DepartmentId,
                DepartmentName = x.Department != null ? x.Department.Name : string.Empty,
                x.Name,
                x.Code,
                x.TeamLeadEmployeeId,
                TeamLeadName = x.TeamLead != null ? x.TeamLead.FirstName + " " + x.TeamLead.LastName : null,
                EmployeeCount = x.Employees.Count,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        return teams.Select(x => new TeamListItemDto(
            x.Id,
            x.DepartmentId,
            x.DepartmentName,
            x.Name,
            x.Code,
            x.TeamLeadEmployeeId,
            x.TeamLeadName,
            x.EmployeeCount,
            x.IsActive)).ToList();
    }

    public async Task<TeamDto> GetTeamByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var team = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.TeamLead)
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Team = x,
                EmployeeCount = x.Employees.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null)
            throw new KeyNotFoundException($"Team '{id}' was not found.");

        return MapToDto(team.Team, team.EmployeeCount);
    }

    public async Task<TeamDto> CreateTeamAsync(
        CreateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var deptExists = await _dbContext.Departments
            .AnyAsync(x => x.Id == request.DepartmentId && x.TenantId == tenantId, cancellationToken);
        if (!deptExists)
            throw new KeyNotFoundException($"Department '{request.DepartmentId}' was not found in the current organization.");

        if (request.TeamLeadEmployeeId.HasValue && request.TeamLeadEmployeeId.Value != Guid.Empty)
        {
            var leadExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.TeamLeadEmployeeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!leadExists)
                throw new KeyNotFoundException($"Team lead employee '{request.TeamLeadEmployeeId}' was not found in the current organization.");
        }

        var codeExists = await _dbContext.Teams
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Team code '{normalizedCode}' already exists.");

        var team = new Team(
            tenantId,
            request.DepartmentId,
            request.Name,
            normalizedCode,
            request.Description,
            request.TeamLeadEmployeeId);

        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Team",
                team.Id.ToString(),
                $"Created team: {team.Name} ({team.Code})"),
            cancellationToken);

        var created = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.TeamLead)
            .FirstAsync(x => x.Id == team.Id, cancellationToken);

        return MapToDto(created, 0);
    }

    public async Task<TeamDto> UpdateTeamAsync(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var team = await _dbContext.Teams
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (team is null)
            throw new KeyNotFoundException($"Team '{id}' was not found.");

        var deptExists = await _dbContext.Departments
            .AnyAsync(x => x.Id == request.DepartmentId && x.TenantId == tenantId, cancellationToken);
        if (!deptExists)
            throw new KeyNotFoundException($"Department '{request.DepartmentId}' was not found in the current organization.");

        if (request.TeamLeadEmployeeId.HasValue && request.TeamLeadEmployeeId.Value != Guid.Empty)
        {
            var leadExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.TeamLeadEmployeeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!leadExists)
                throw new KeyNotFoundException($"Team lead employee '{request.TeamLeadEmployeeId}' was not found in the current organization.");
        }

        var codeExists = await _dbContext.Teams
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Team code '{normalizedCode}' already exists.");

        team.Update(
            request.DepartmentId,
            request.Name,
            normalizedCode,
            request.Description,
            request.TeamLeadEmployeeId);

        team.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Team",
                team.Id.ToString(),
                $"Updated team: {team.Name} ({team.Code})"),
            cancellationToken);

        var updated = await _dbContext.Teams
            .AsNoTracking()
            .Include(x => x.Department)
            .Include(x => x.TeamLead)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Team = x,
                EmployeeCount = x.Employees.Count
            })
            .FirstAsync(cancellationToken);

        return MapToDto(updated.Team, updated.EmployeeCount);
    }

    public async Task DeleteTeamAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var team = await _dbContext.Teams
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (team is null)
            throw new KeyNotFoundException($"Team '{id}' was not found.");

        var hasEmployees = await _dbContext.Employees
            .AnyAsync(x => x.TeamId == id && x.TenantId == tenantId, cancellationToken);
        if (hasEmployees)
            throw new InvalidOperationException("Cannot delete team that still has assigned employees.");

        team.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Team",
                team.Id.ToString(),
                $"Deleted team: {team.Name} ({team.Code})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static TeamDto MapToDto(Team team, int employeeCount) =>
        new(
            team.Id,
            team.TenantId,
            team.DepartmentId,
            team.Department?.Name ?? string.Empty,
            team.Name,
            team.Code,
            team.Description,
            team.TeamLeadEmployeeId,
            team.TeamLead != null ? $"{team.TeamLead.FirstName} {team.TeamLead.LastName}".Trim() : null,
            employeeCount,
            team.IsActive,
            team.CreatedAtUtc,
            team.UpdatedAtUtc);
}
