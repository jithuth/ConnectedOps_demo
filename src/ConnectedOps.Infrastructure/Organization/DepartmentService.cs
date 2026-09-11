using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class DepartmentService : IDepartmentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DepartmentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<DepartmentListItemDto>> GetDepartmentsAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.Departments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.ParentDepartment)
            .Where(x => x.TenantId == tenantId);

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        var departments = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.BranchId,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                x.ParentDepartmentId,
                ParentDepartmentName = x.ParentDepartment != null ? x.ParentDepartment.Name : null,
                x.Name,
                x.Code,
                TeamCount = x.Teams.Count,
                EmployeeCount = x.Employees.Count,
                x.IsActive
            })
            .ToListAsync(cancellationToken);

        return departments.Select(x => new DepartmentListItemDto(
            x.Id,
            x.BranchId,
            x.BranchName,
            x.ParentDepartmentId,
            x.ParentDepartmentName,
            x.Name,
            x.Code,
            x.TeamCount,
            x.EmployeeCount,
            x.IsActive)).ToList();
    }

    public async Task<DepartmentDto> GetDepartmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var dept = await _dbContext.Departments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.ParentDepartment)
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Department = x,
                TeamCount = x.Teams.Count,
                EmployeeCount = x.Employees.Count,
                SubDepartmentCount = x.SubDepartments.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dept is null)
            throw new KeyNotFoundException($"Department '{id}' was not found.");

        return MapToDto(dept.Department, dept.TeamCount, dept.EmployeeCount, dept.SubDepartmentCount);
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(
        CreateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");
        }

        if (request.ParentDepartmentId.HasValue && request.ParentDepartmentId.Value != Guid.Empty)
        {
            var parentExists = await _dbContext.Departments
                .AnyAsync(x => x.Id == request.ParentDepartmentId.Value && x.TenantId == tenantId, cancellationToken);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent department '{request.ParentDepartmentId}' was not found in the current organization.");
        }

        var codeExists = await _dbContext.Departments
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Department code '{normalizedCode}' already exists.");

        var department = new Department(
            tenantId,
            request.Name,
            normalizedCode,
            request.BranchId,
            request.ParentDepartmentId,
            request.Description);

        _dbContext.Departments.Add(department);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Department",
                department.Id.ToString(),
                $"Created department: {department.Name} ({department.Code})"),
            cancellationToken);

        var created = await _dbContext.Departments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.ParentDepartment)
            .FirstAsync(x => x.Id == department.Id, cancellationToken);

        return MapToDto(created, 0, 0, 0);
    }

    public async Task<DepartmentDto> UpdateDepartmentAsync(
        Guid id,
        UpdateDepartmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (department is null)
            throw new KeyNotFoundException($"Department '{id}' was not found.");

        if (request.ParentDepartmentId.HasValue && request.ParentDepartmentId.Value == id)
            throw new InvalidOperationException("A department cannot be its own parent.");

        if (request.BranchId.HasValue && request.BranchId.Value != Guid.Empty)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");
        }

        if (request.ParentDepartmentId.HasValue && request.ParentDepartmentId.Value != Guid.Empty)
        {
            var parentExists = await _dbContext.Departments
                .AnyAsync(x => x.Id == request.ParentDepartmentId.Value && x.TenantId == tenantId, cancellationToken);
            if (!parentExists)
                throw new KeyNotFoundException($"Parent department '{request.ParentDepartmentId}' was not found in the current organization.");
        }

        var codeExists = await _dbContext.Departments
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Department code '{normalizedCode}' already exists.");

        department.Update(
            request.Name,
            normalizedCode,
            request.BranchId,
            request.ParentDepartmentId,
            request.Description);

        department.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Department",
                department.Id.ToString(),
                $"Updated department: {department.Name} ({department.Code})"),
            cancellationToken);

        var updated = await _dbContext.Departments
            .AsNoTracking()
            .Include(x => x.Branch)
            .Include(x => x.ParentDepartment)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                Department = x,
                TeamCount = x.Teams.Count,
                EmployeeCount = x.Employees.Count,
                SubDepartmentCount = x.SubDepartments.Count
            })
            .FirstAsync(cancellationToken);

        return MapToDto(updated.Department, updated.TeamCount, updated.EmployeeCount, updated.SubDepartmentCount);
    }

    public async Task DeleteDepartmentAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var department = await _dbContext.Departments
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (department is null)
            throw new KeyNotFoundException($"Department '{id}' was not found.");

        var hasSubDepts = await _dbContext.Departments
            .AnyAsync(x => x.ParentDepartmentId == id && x.TenantId == tenantId, cancellationToken);
        if (hasSubDepts)
            throw new InvalidOperationException("Cannot delete department that still has sub-departments.");

        var hasTeams = await _dbContext.Teams
            .AnyAsync(x => x.DepartmentId == id && x.TenantId == tenantId, cancellationToken);
        if (hasTeams)
            throw new InvalidOperationException("Cannot delete department that still has teams.");

        var hasEmployees = await _dbContext.Employees
            .AnyAsync(x => x.DepartmentId == id && x.TenantId == tenantId, cancellationToken);
        if (hasEmployees)
            throw new InvalidOperationException("Cannot delete department that still has assigned employees.");

        department.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Department",
                department.Id.ToString(),
                $"Deleted department: {department.Name} ({department.Code})"),
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

    private static DepartmentDto MapToDto(Department dept, int teamCount, int employeeCount, int subDepartmentCount) =>
        new(
            dept.Id,
            dept.TenantId,
            dept.BranchId,
            dept.Branch?.Name,
            dept.ParentDepartmentId,
            dept.ParentDepartment?.Name,
            dept.Name,
            dept.Code,
            dept.Description,
            dept.IsActive,
            teamCount,
            employeeCount,
            subDepartmentCount,
            dept.CreatedAtUtc,
            dept.UpdatedAtUtc);
}
