using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class CorrectiveActionService : ICorrectiveActionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public CorrectiveActionService(
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

    public async Task<PagedResult<CorrectiveActionDto>> GetPagedAsync(CorrectiveActionFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.CorrectiveActions
            .AsNoTracking()
            .Include(x => x.AssignedEmployee)
            .Include(x => x.SafetyIncident)
            .Include(x => x.SafetyViolation)
            .Include(x => x.ComplianceRecord)
                .ThenInclude(r => r!.ComplianceRequirement)
            .Where(x => x.TenantId == tenantId);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(x => x.Priority == filter.Priority.Value);

        if (filter.AssignedEmployeeId.HasValue)
            query = query.Where(x => x.AssignedEmployeeId == filter.AssignedEmployeeId.Value);

        if (filter.SafetyIncidentId.HasValue)
            query = query.Where(x => x.SafetyIncidentId == filter.SafetyIncidentId.Value);

        if (filter.SafetyViolationId.HasValue)
            query = query.Where(x => x.SafetyViolationId == filter.SafetyViolationId.Value);

        if (filter.ComplianceRecordId.HasValue)
            query = query.Where(x => x.ComplianceRecordId == filter.ComplianceRecordId.Value);

        var now = DateTime.UtcNow;
        if (filter.OverdueOnly == true)
        {
            query = query.Where(x => x.DueDateUtc.HasValue && x.DueDateUtc.Value < now && (x.Status == CorrectiveActionStatus.Open || x.Status == CorrectiveActionStatus.InProgress || x.Status == CorrectiveActionStatus.Overdue));
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(term) ||
                x.Description.ToLower().Contains(term) ||
                (x.AssignedEmployee != null && (x.AssignedEmployee.FirstName.ToLower().Contains(term) || x.AssignedEmployee.LastName.ToLower().Contains(term))) ||
                (x.SafetyIncident != null && x.SafetyIncident.IncidentNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<CorrectiveActionDto>(items, totalCount, page, pageSize);
    }

    public async Task<CorrectiveActionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Include(x => x.AssignedEmployee)
            .Include(x => x.SafetyIncident)
            .Include(x => x.SafetyViolation)
            .Include(x => x.ComplianceRecord)
                .ThenInclude(r => r!.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<CorrectiveActionDto> CreateAsync(CreateCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        if (request.AssignedEmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees.AnyAsync(e => e.Id == request.AssignedEmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!empExists) throw new NotFoundException(nameof(Employee), request.AssignedEmployeeId.Value.ToString());
        }

        if (request.SafetyIncidentId.HasValue)
        {
            var incExists = await _dbContext.SafetyIncidents.AnyAsync(i => i.Id == request.SafetyIncidentId.Value && i.TenantId == tenantId, cancellationToken);
            if (!incExists) throw new NotFoundException(nameof(SafetyIncident), request.SafetyIncidentId.Value.ToString());
        }

        if (request.SafetyViolationId.HasValue)
        {
            var vioExists = await _dbContext.SafetyViolations.AnyAsync(v => v.Id == request.SafetyViolationId.Value && v.TenantId == tenantId, cancellationToken);
            if (!vioExists) throw new NotFoundException(nameof(SafetyViolation), request.SafetyViolationId.Value.ToString());
        }

        if (request.ComplianceRecordId.HasValue)
        {
            var recExists = await _dbContext.ComplianceRecords.AnyAsync(r => r.Id == request.ComplianceRecordId.Value && r.TenantId == tenantId, cancellationToken);
            if (!recExists) throw new NotFoundException(nameof(ComplianceRecord), request.ComplianceRecordId.Value.ToString());
        }

        var entity = new CorrectiveAction(
            tenantId,
            request.Title,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            request.AssignedEmployeeId,
            request.SafetyIncidentId,
            request.SafetyViolationId,
            request.ComplianceRecordId,
            request.VerificationRequired,
            CorrectiveActionStatus.Open,
            _currentUserContext.UserId);

        _dbContext.CorrectiveActions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.CorrectiveActionCreated,
            nameof(CorrectiveAction),
            entity.Id.ToString(),
            $"Created corrective action '{entity.Title}' (Priority: {entity.Priority})",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CorrectiveActionDto> UpdateAsync(Guid id, UpdateCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        if (request.AssignedEmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees.AnyAsync(e => e.Id == request.AssignedEmployeeId.Value && e.TenantId == tenantId, cancellationToken);
            if (!empExists) throw new NotFoundException(nameof(Employee), request.AssignedEmployeeId.Value.ToString());
        }

        entity.Update(
            request.Title,
            request.Description,
            request.Priority,
            request.DueDateUtc,
            request.AssignedEmployeeId,
            request.VerificationRequired,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.CorrectiveActionUpdated,
            nameof(CorrectiveAction),
            entity.Id.ToString(),
            $"Updated corrective action '{entity.Title}'",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CorrectiveActionDto> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        entity.Start(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CorrectiveActionDto> CompleteAsync(Guid id, CompleteCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        var completerId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");
        entity.Complete(completerId, request.ResolutionNotes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.CorrectiveActionCompleted,
            nameof(CorrectiveAction),
            entity.Id.ToString(),
            $"Completed corrective action '{entity.Title}'"));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CorrectiveActionDto> VerifyAsync(Guid id, VerifyCorrectiveActionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        var verifierId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");
        entity.Verify(verifierId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.CorrectiveActionVerified,
            nameof(CorrectiveAction),
            entity.Id.ToString(),
            $"Verified corrective action '{entity.Title}'"));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CorrectiveActionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        entity.Cancel(_currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.CorrectiveActionCancelled,
            nameof(CorrectiveAction),
            entity.Id.ToString(),
            $"Cancelled corrective action '{entity.Title}'"));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.CorrectiveActions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(CorrectiveAction), id.ToString());

        entity.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CorrectiveActionDto>> GetByIncidentIdAsync(Guid incidentId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var items = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .Include(x => x.AssignedEmployee)
            .Include(x => x.SafetyIncident)
            .Include(x => x.SafetyViolation)
            .Include(x => x.ComplianceRecord)
            .Where(x => x.TenantId == tenantId && x.SafetyIncidentId == incidentId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    private static CorrectiveActionDto MapToDto(CorrectiveAction c)
    {
        bool isOverdue = c.DueDateUtc.HasValue && c.DueDateUtc.Value < DateTime.UtcNow &&
                         (c.Status == CorrectiveActionStatus.Open || c.Status == CorrectiveActionStatus.InProgress || c.Status == CorrectiveActionStatus.Overdue);

        return new CorrectiveActionDto(
            c.Id,
            c.TenantId,
            c.Title,
            c.Description,
            c.Priority,
            c.DueDateUtc,
            c.AssignedEmployeeId,
            c.AssignedEmployee != null ? $"{c.AssignedEmployee.FirstName} {c.AssignedEmployee.LastName}" : null,
            c.SafetyIncidentId,
            c.SafetyIncident?.IncidentNumber,
            c.SafetyViolationId,
            c.SafetyViolation?.Description,
            c.ComplianceRecordId,
            c.ComplianceRecord?.ComplianceRequirement?.Name,
            isOverdue && c.Status != CorrectiveActionStatus.Completed && c.Status != CorrectiveActionStatus.Verified ? CorrectiveActionStatus.Overdue : c.Status,
            isOverdue,
            c.CompletedAtUtc,
            c.CompletedByUserId,
            null,
            c.VerificationRequired,
            c.VerifiedAtUtc,
            c.VerifiedByUserId,
            null,
            c.ResolutionNotes,
            c.CreatedAtUtc);
    }
}
