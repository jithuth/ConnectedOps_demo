using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class EmployeeService : IEmployeeService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public EmployeeService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<EmployeeListItemDto>> GetEmployeesAsync(
        Guid? branchId = null,
        Guid? departmentId = null,
        Guid? teamId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.Manager)
            .Where(x => x.TenantId == tenantId);

        if (branchId.HasValue && branchId.Value != Guid.Empty)
            query = query.Where(x => x.BranchId == branchId.Value);

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
            query = query.Where(x => x.DepartmentId == departmentId.Value);

        if (teamId.HasValue && teamId.Value != Guid.Empty)
            query = query.Where(x => x.TeamId == teamId.Value);

        var employees = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToListAsync(cancellationToken);

        return employees.Select(x => new EmployeeListItemDto(
            x.Id,
            x.EmployeeNumber,
            $"{x.FirstName} {x.LastName}".Trim(),
            x.Email,
            x.Phone,
            x.JobTitle,
            x.EmploymentType,
            x.EmploymentType.ToString(),
            x.EmploymentStatus,
            x.EmploymentStatus.ToString(),
            x.Branch?.Name,
            x.Department?.Name,
            x.Team?.Name,
            x.Manager != null ? $"{x.Manager.FirstName} {x.Manager.LastName}".Trim() : null,
            x.UserId.HasValue,
            x.IsActive)).ToList();
    }

    public async Task<EmployeeDto> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var employee = await _dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.Manager)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException($"Employee '{id}' was not found.");

        return MapToDto(employee);
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedNumber = request.EmployeeNumber.Trim().ToUpperInvariant();

        var numberExists = await _dbContext.Employees
            .AnyAsync(x => x.TenantId == tenantId && x.EmployeeNumber == normalizedNumber, cancellationToken);

        if (numberExists)
            throw new ConflictException($"Employee number '{normalizedNumber}' already exists.");

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");
        }

        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(x => x.Id == request.DepartmentId.Value && x.TenantId == tenantId, cancellationToken);
            if (!deptExists)
                throw new KeyNotFoundException($"Department '{request.DepartmentId}' was not found in the current organization.");
        }

        if (request.TeamId.HasValue && request.TeamId.Value != Guid.Empty)
        {
            var teamExists = await _dbContext.Teams
                .AnyAsync(x => x.Id == request.TeamId.Value && x.TenantId == tenantId, cancellationToken);
            if (!teamExists)
                throw new KeyNotFoundException($"Team '{request.TeamId}' was not found in the current organization.");
        }

        if (request.ManagerEmployeeId.HasValue && request.ManagerEmployeeId.Value != Guid.Empty)
        {
            var mgrExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.ManagerEmployeeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!mgrExists)
                throw new KeyNotFoundException($"Manager employee '{request.ManagerEmployeeId}' was not found in the current organization.");
        }

        if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
        {
            var userInTenant = await _dbContext.TenantUsers
                .AnyAsync(x => x.UserId == request.UserId.Value && x.TenantId == tenantId && x.IsActive, cancellationToken);
            if (!userInTenant)
                throw new KeyNotFoundException($"User '{request.UserId}' was not found in the current organization.");

            var userAlreadyLinked = await _dbContext.Employees
                .AnyAsync(x => x.TenantId == tenantId && x.UserId == request.UserId.Value, cancellationToken);
            if (userAlreadyLinked)
                throw new ConflictException("User is already linked to another employee.");
        }

        var employee = new Employee(
            tenantId,
            normalizedNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.EmploymentType,
            request.EmploymentStatus,
            request.BranchId,
            request.DepartmentId,
            request.TeamId,
            request.ManagerEmployeeId,
            request.UserId,
            request.HireDate);

        _dbContext.Employees.Add(employee);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Employee",
                employee.Id.ToString(),
                $"Created employee: {employee.FullName} ({employee.EmployeeNumber})"),
            cancellationToken);

        var created = await _dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.Manager)
            .FirstAsync(x => x.Id == employee.Id, cancellationToken);

        return MapToDto(created);
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(
        Guid id,
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;
        var normalizedNumber = request.EmployeeNumber.Trim().ToUpperInvariant();

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException($"Employee '{id}' was not found.");

        if (request.ManagerEmployeeId.HasValue && request.ManagerEmployeeId.Value == id)
            throw new InvalidOperationException("An employee cannot be their own manager.");

        var numberExists = await _dbContext.Employees
            .AnyAsync(x => x.TenantId == tenantId && x.EmployeeNumber == normalizedNumber && x.Id != id, cancellationToken);

        if (numberExists)
            throw new ConflictException($"Employee number '{normalizedNumber}' already exists.");

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");
        }

        if (request.DepartmentId.HasValue && request.DepartmentId.Value != Guid.Empty)
        {
            var deptExists = await _dbContext.Departments
                .AnyAsync(x => x.Id == request.DepartmentId.Value && x.TenantId == tenantId, cancellationToken);
            if (!deptExists)
                throw new KeyNotFoundException($"Department '{request.DepartmentId}' was not found in the current organization.");
        }

        if (request.TeamId.HasValue && request.TeamId.Value != Guid.Empty)
        {
            var teamExists = await _dbContext.Teams
                .AnyAsync(x => x.Id == request.TeamId.Value && x.TenantId == tenantId, cancellationToken);
            if (!teamExists)
                throw new KeyNotFoundException($"Team '{request.TeamId}' was not found in the current organization.");
        }

        if (request.ManagerEmployeeId.HasValue && request.ManagerEmployeeId.Value != Guid.Empty)
        {
            var mgrExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.ManagerEmployeeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!mgrExists)
                throw new KeyNotFoundException($"Manager employee '{request.ManagerEmployeeId}' was not found in the current organization.");
        }

        employee.Update(
            normalizedNumber,
            request.FirstName,
            request.LastName,
            request.Email,
            request.Phone,
            request.JobTitle,
            request.EmploymentType,
            request.EmploymentStatus,
            request.BranchId,
            request.DepartmentId,
            request.TeamId,
            request.ManagerEmployeeId,
            request.HireDate,
            request.TerminationDate);

        employee.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Employee",
                employee.Id.ToString(),
                $"Updated employee: {employee.FullName} ({employee.EmployeeNumber})"),
            cancellationToken);

        var updated = await _dbContext.Employees
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.Department)
            .Include(x => x.Team)
            .Include(x => x.Manager)
            .FirstAsync(x => x.Id == id, cancellationToken);

        return MapToDto(updated);
    }

    public async Task DeleteEmployeeAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException($"Employee '{id}' was not found.");

        var hasDirectReports = await _dbContext.Employees
            .AnyAsync(x => x.ManagerEmployeeId == id && x.TenantId == tenantId, cancellationToken);
        if (hasDirectReports)
            throw new InvalidOperationException("Cannot delete employee who has direct reports. Reassign direct reports first.");

        var isTeamLead = await _dbContext.Teams
            .AnyAsync(x => x.TeamLeadEmployeeId == id && x.TenantId == tenantId, cancellationToken);
        if (isTeamLead)
            throw new InvalidOperationException("Cannot delete employee who is a team lead. Reassign team lead first.");

        employee.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Employee",
                employee.Id.ToString(),
                $"Deleted employee: {employee.FullName} ({employee.EmployeeNumber})"),
            cancellationToken);
    }

    public async Task LinkUserAsync(
        Guid employeeId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException($"Employee '{employeeId}' was not found.");

        var userInTenant = await _dbContext.TenantUsers
            .AnyAsync(x => x.UserId == userId && x.TenantId == tenantId && x.IsActive, cancellationToken);
        if (!userInTenant)
            throw new KeyNotFoundException($"User '{userId}' was not found in the current organization.");

        var alreadyLinked = await _dbContext.Employees
            .AnyAsync(x => x.TenantId == tenantId && x.UserId == userId && x.Id != employeeId, cancellationToken);
        if (alreadyLinked)
            throw new ConflictException("User is already linked to another employee in this organization.");

        employee.LinkUser(userId);
        employee.MarkUpdated(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Linked,
                "Employee",
                employee.Id.ToString(),
                $"Linked user '{userId}' to employee: {employee.FullName} ({employee.EmployeeNumber})"),
            cancellationToken);
    }

    public async Task UnlinkUserAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var currentUserId = _currentUserContext.UserId;

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.TenantId == tenantId, cancellationToken);

        if (employee is null)
            throw new KeyNotFoundException($"Employee '{employeeId}' was not found.");

        var oldUserId = employee.UserId;
        employee.UnlinkUser();
        employee.MarkUpdated(currentUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Unlinked,
                "Employee",
                employee.Id.ToString(),
                $"Unlinked user '{oldUserId}' from employee: {employee.FullName} ({employee.EmployeeNumber})"),
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

    private static EmployeeDto MapToDto(Employee employee) =>
        new(
            employee.Id,
            employee.TenantId,
            employee.UserId,
            employee.BranchId,
            employee.Branch?.Name,
            employee.DepartmentId,
            employee.Department?.Name,
            employee.TeamId,
            employee.Team?.Name,
            employee.ManagerEmployeeId,
            employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}".Trim() : null,
            employee.EmployeeNumber,
            employee.FirstName,
            employee.LastName,
            $"{employee.FirstName} {employee.LastName}".Trim(),
            employee.Email,
            employee.Phone,
            employee.JobTitle,
            employee.EmploymentType,
            employee.EmploymentType.ToString(),
            employee.EmploymentStatus,
            employee.EmploymentStatus.ToString(),
            employee.HireDate,
            employee.TerminationDate,
            employee.IsActive,
            employee.CreatedAtUtc,
            employee.UpdatedAtUtc);
}
