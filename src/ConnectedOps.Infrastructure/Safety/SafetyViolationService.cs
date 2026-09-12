using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class SafetyViolationService : ISafetyViolationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public SafetyViolationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<PagedResult<SafetyViolationDto>> GetPagedAsync(SafetyViolationFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.SafetyViolations
            .AsNoTracking()
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Include(x => x.SafetyIncident)
            .Where(x => x.TenantId == tenantId);

        if (filter.ViolationType.HasValue)
            query = query.Where(x => x.ViolationType == filter.ViolationType.Value);

        if (filter.Severity.HasValue)
            query = query.Where(x => x.Severity == filter.Severity.Value);

        if (filter.Source.HasValue)
            query = query.Where(x => x.Source == filter.Source.Value);

        if (filter.IsResolved.HasValue)
            query = query.Where(x => x.IsResolved == filter.IsResolved.Value);

        if (filter.DriverId.HasValue)
            query = query.Where(x => x.DriverId == filter.DriverId.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(x => x.VehicleId == filter.VehicleId.Value);

        if (filter.EmployeeId.HasValue)
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);

        if (filter.SafetyIncidentId.HasValue)
            query = query.Where(x => x.SafetyIncidentId == filter.SafetyIncidentId.Value);

        if (filter.FromUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc >= filter.FromUtc.Value);

        if (filter.ToUtc.HasValue)
            query = query.Where(x => x.OccurredAtUtc <= filter.ToUtc.Value);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Description.ToLower().Contains(term) ||
                (x.Reference != null && x.Reference.ToLower().Contains(term)) ||
                (x.Driver != null && (x.Driver.FirstName.ToLower().Contains(term) || x.Driver.LastName.ToLower().Contains(term))) ||
                (x.Vehicle != null && ((x.Vehicle.RegistrationNumber != null && x.Vehicle.RegistrationNumber.ToLower().Contains(term)) || (x.Vehicle.VIN != null && x.Vehicle.VIN.ToLower().Contains(term)) || x.Vehicle.VehicleNumber.ToLower().Contains(term))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<SafetyViolationDto>(items, totalCount, page, pageSize);
    }

    public async Task<SafetyViolationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyViolations
            .AsNoTracking()
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Include(x => x.SafetyIncident)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<SafetyViolationDto> CreateAsync(CreateSafetyViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        if (request.DriverId.HasValue)
        {
            var exists = await _dbContext.Drivers.AnyAsync(d => d.Id == request.DriverId.Value && d.TenantId == tenantId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(Driver), request.DriverId.Value.ToString());
        }

        if (request.VehicleId.HasValue)
        {
            var exists = await _dbContext.Vehicles.AnyAsync(v => v.Id == request.VehicleId.Value && v.TenantId == tenantId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(Vehicle), request.VehicleId.Value.ToString());
        }

        if (request.EmployeeId.HasValue)
        {
            var exists = await _dbContext.Employees.AnyAsync(e => e.Id == request.EmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!exists) throw new NotFoundException(nameof(Employee), request.EmployeeId.Value.ToString());
        }

        var entity = new SafetyViolation(
            tenantId,
            request.ViolationType,
            request.Severity,
            request.Source,
            request.OccurredAtUtc,
            request.Description,
            request.DriverId,
            request.EmployeeId,
            request.VehicleId,
            request.SafetyIncidentId,
            request.Reference,
            request.Notes,
            _currentUserContext.UserId);

        _dbContext.SafetyViolations.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyViolationCreated,
            nameof(SafetyViolation),
            entity.Id.ToString(),
            $"Created safety violation: {entity.ViolationType} ({entity.Severity}) from source {entity.Source}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyViolationDto> UpdateAsync(Guid id, UpdateSafetyViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyViolations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyViolation), id.ToString());

        entity.Update(
            request.ViolationType,
            request.Severity,
            request.Source,
            request.OccurredAtUtc,
            request.Description,
            request.DriverId,
            request.EmployeeId,
            request.VehicleId,
            request.SafetyIncidentId,
            request.Reference,
            request.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyViolationUpdated,
            nameof(SafetyViolation),
            entity.Id.ToString(),
            $"Updated safety violation {entity.Id}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<SafetyViolationDto> ResolveAsync(Guid id, ResolveSafetyViolationRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyViolations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyViolation), id.ToString());

        var resolverId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");
        entity.Resolve(resolverId, request.ResolutionNotes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.SafetyViolationResolved,
            nameof(SafetyViolation),
            entity.Id.ToString(),
            $"Resolved safety violation {entity.Id} ({entity.ViolationType})"));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.SafetyViolations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(SafetyViolation), id.ToString());

        entity.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SafetyViolationDto>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var items = await _dbContext.SafetyViolations
            .AsNoTracking()
            .Include(x => x.Driver)
            .Include(x => x.Employee)
            .Include(x => x.Vehicle)
            .Include(x => x.SafetyIncident)
            .Where(x => x.TenantId == tenantId && x.SafetyIncidentId == incidentId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    private static SafetyViolationDto MapToDto(SafetyViolation v) =>
        new(
            v.Id,
            v.TenantId,
            v.ViolationType,
            v.Severity,
            v.Source,
            v.OccurredAtUtc,
            v.Description,
            v.DriverId,
            v.Driver?.DisplayName,
            v.EmployeeId,
            v.Employee != null ? $"{v.Employee.FirstName} {v.Employee.LastName}" : null,
            v.VehicleId,
            v.Vehicle != null ? (v.Vehicle.RegistrationNumber ?? v.Vehicle.VehicleNumber) : null,
            v.SafetyIncidentId,
            v.SafetyIncident?.IncidentNumber,
            v.Reference,
            v.IsResolved,
            v.ResolvedAtUtc,
            v.ResolvedByUserId,
            null,
            v.ResolutionNotes,
            v.Notes,
            v.CreatedAtUtc);
}
