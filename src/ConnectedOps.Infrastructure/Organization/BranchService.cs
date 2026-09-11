using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class BranchService : IBranchService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public BranchService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<BranchListItemDto>> GetBranchesAsync(
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Code,
                x.Type,
                x.IsHeadOffice,
                x.IsActive,
                x.City,
                x.CountryCode,
                LocationCount = x.Locations.Count,
                EmployeeCount = x.Employees.Count
            })
            .OrderByDescending(x => x.IsHeadOffice)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return branches.Select(x => new BranchListItemDto(
            x.Id,
            x.Name,
            x.Code,
            x.Type,
            x.Type.ToString(),
            x.IsHeadOffice,
            x.IsActive,
            x.City,
            x.CountryCode,
            x.LocationCount,
            x.EmployeeCount)).ToList();
    }

    public async Task<BranchDto> GetBranchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var branch = await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Branch = x,
                LocationCount = x.Locations.Count,
                DepartmentCount = x.Departments.Count,
                EmployeeCount = x.Employees.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (branch is null)
            throw new KeyNotFoundException($"Branch '{id}' was not found.");

        return MapToDto(branch.Branch, branch.LocationCount, branch.DepartmentCount, branch.EmployeeCount);
    }

    public async Task<BranchDto> CreateBranchAsync(
        CreateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.Branches
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Branch code '{normalizedCode}' already exists.");

        if (request.IsHeadOffice)
        {
            var headOfficeExists = await _dbContext.Branches
                .AnyAsync(x => x.TenantId == tenantId && x.IsHeadOffice, cancellationToken);

            if (headOfficeExists)
                throw new ConflictException("A head office branch already exists in this organization.");
        }

        var branch = new Branch(
            tenantId,
            request.Name,
            normalizedCode,
            request.Type,
            request.IsHeadOffice,
            request.Email,
            request.Phone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode,
            request.TimeZoneId);

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Branch",
                branch.Id.ToString(),
                $"Created branch: {branch.Name} ({branch.Code})"),
            cancellationToken);

        return MapToDto(branch, 0, 0, 0);
    }

    public async Task<BranchDto> UpdateBranchAsync(
        Guid id,
        UpdateBranchRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (branch is null)
            throw new KeyNotFoundException($"Branch '{id}' was not found.");

        var codeExists = await _dbContext.Branches
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Branch code '{normalizedCode}' already exists.");

        if (request.IsHeadOffice && !branch.IsHeadOffice)
        {
            var headOfficeExists = await _dbContext.Branches
                .AnyAsync(x => x.TenantId == tenantId && x.IsHeadOffice && x.Id != id, cancellationToken);

            if (headOfficeExists)
                throw new ConflictException("A head office branch already exists in this organization.");
        }

        branch.Update(
            request.Name,
            normalizedCode,
            request.Type,
            request.IsHeadOffice,
            request.Email,
            request.Phone,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode,
            request.TimeZoneId);

        branch.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Branch",
                branch.Id.ToString(),
                $"Updated branch: {branch.Name} ({branch.Code})"),
            cancellationToken);

        var counts = await _dbContext.Branches
            .Where(x => x.Id == id)
            .Select(x => new
            {
                LocationCount = x.Locations.Count,
                DepartmentCount = x.Departments.Count,
                EmployeeCount = x.Employees.Count
            })
            .FirstAsync(cancellationToken);

        return MapToDto(branch, counts.LocationCount, counts.DepartmentCount, counts.EmployeeCount);
    }

    public async Task DeleteBranchAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var branch = await _dbContext.Branches
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (branch is null)
            throw new KeyNotFoundException($"Branch '{id}' was not found.");

        var hasLocations = await _dbContext.Locations
            .AnyAsync(x => x.BranchId == id && x.TenantId == tenantId, cancellationToken);

        if (hasLocations)
            throw new InvalidOperationException("Cannot delete branch that still has assigned locations.");

        var hasDepartments = await _dbContext.Departments
            .AnyAsync(x => x.BranchId == id && x.TenantId == tenantId, cancellationToken);

        if (hasDepartments)
            throw new InvalidOperationException("Cannot delete branch that still has assigned departments.");

        var hasEmployees = await _dbContext.Employees
            .AnyAsync(x => x.BranchId == id && x.TenantId == tenantId, cancellationToken);

        if (hasEmployees)
            throw new InvalidOperationException("Cannot delete branch that still has assigned employees.");

        branch.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Branch",
                branch.Id.ToString(),
                $"Deleted branch: {branch.Name} ({branch.Code})"),
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

    private static BranchDto MapToDto(Branch branch, int locationCount, int departmentCount, int employeeCount) =>
        new(
            branch.Id,
            branch.TenantId,
            branch.Name,
            branch.Code,
            branch.Type,
            branch.Type.ToString(),
            branch.IsHeadOffice,
            branch.IsActive,
            branch.Email,
            branch.Phone,
            branch.AddressLine1,
            branch.AddressLine2,
            branch.City,
            branch.StateOrProvince,
            branch.PostalCode,
            branch.CountryCode,
            branch.TimeZoneId,
            locationCount,
            departmentCount,
            employeeCount,
            branch.CreatedAtUtc,
            branch.UpdatedAtUtc);
}
