using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Compliance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceExceptionService : IComplianceExceptionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public ComplianceExceptionService(
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

    public async Task<PagedResult<ComplianceExceptionDto>> GetPagedAsync(ComplianceExceptionFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.ComplianceExceptions
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .Where(x => x.TenantId == tenantId);

        if (filter.SubjectType.HasValue)
            query = query.Where(x => x.SubjectType == filter.SubjectType.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(x => x.VehicleId == filter.VehicleId.Value);

        if (filter.DriverId.HasValue)
            query = query.Where(x => x.DriverId == filter.DriverId.Value);

        if (filter.AssetId.HasValue)
            query = query.Where(x => x.AssetId == filter.AssetId.Value);

        if (filter.ComplianceRequirementId.HasValue)
            query = query.Where(x => x.ComplianceRequirementId == filter.ComplianceRequirementId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        if (filter.ActiveOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(x => x.Status == ComplianceExceptionStatus.Approved && x.EffectiveFromUtc <= now && now <= x.EffectiveToUtc);
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

        return new PagedResult<ComplianceExceptionDto>(items, totalCount, page, pageSize);
    }

    public async Task<ComplianceExceptionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ComplianceExceptionDto> CreateAsync(CreateComplianceExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var approverId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");

        var requirement = await _dbContext.ComplianceRequirements
            .FirstOrDefaultAsync(x => x.Id == request.ComplianceRequirementId && x.TenantId == tenantId, cancellationToken);

        if (requirement == null)
            throw new NotFoundException(nameof(ComplianceRequirement), request.ComplianceRequirementId.ToString());

        var entity = new ComplianceException(
            tenantId,
            request.ComplianceRequirementId,
            request.SubjectType,
            request.Reason,
            approverId,
            request.EffectiveFromUtc,
            request.EffectiveToUtc,
            request.VehicleId,
            request.DriverId,
            request.AssetId,
            request.Status,
            _currentUserContext.UserId);

        _dbContext.ComplianceExceptions.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceExceptionCreated,
            nameof(ComplianceException),
            entity.Id.ToString(),
            $"Created compliance exemption for requirement {requirement.Code} ({request.Reason})",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ComplianceExceptionDto> ApproveAsync(Guid id, ApproveComplianceExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var approverId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");

        var entity = await _dbContext.ComplianceExceptions
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceException), id.ToString());

        entity.Approve(approverId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceExceptionApproved,
            nameof(ComplianceException),
            entity.Id.ToString(),
            $"Approved compliance exemption for requirement {entity.ComplianceRequirement.Code}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ComplianceExceptionDto> RejectAsync(Guid id, RejectComplianceExceptionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var approverId = _currentUserContext.UserId ?? throw new UnauthorizedAccessException("User ID is required.");

        var entity = await _dbContext.ComplianceExceptions
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceException), id.ToString());

        entity.Reject(approverId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceExceptionRejected,
            nameof(ComplianceException),
            entity.Id.ToString(),
            $"Rejected compliance exemption for requirement {entity.ComplianceRequirement.Code}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ComplianceExceptionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceExceptions
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceException), id.ToString());

        entity.Cancel(_currentUserContext.UserId ?? Guid.Empty);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceExceptionCancelled,
            nameof(ComplianceException),
            entity.Id.ToString(),
            $"Cancelled compliance exemption for requirement {entity.ComplianceRequirement.Code}"));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<bool> HasActiveExceptionAsync(Guid requirementId, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var now = DateTime.UtcNow;

        return await _dbContext.ComplianceExceptions
            .AsNoTracking()
            .AnyAsync(x =>
                x.TenantId == tenantId &&
                x.ComplianceRequirementId == requirementId &&
                x.Status == ComplianceExceptionStatus.Approved &&
                x.EffectiveFromUtc <= now &&
                now <= x.EffectiveToUtc &&
                (x.VehicleId == subjectId || x.DriverId == subjectId || x.AssetId == subjectId),
                cancellationToken);
    }

    private static ComplianceExceptionDto MapToDto(ComplianceException e)
    {
        string? vehicleName = e.Vehicle != null ? $"{e.Vehicle.RegistrationNumber ?? e.Vehicle.VehicleNumber} ({e.Vehicle.DisplayName})" : null;
        string? driverName = e.Driver != null ? e.Driver.DisplayName : null;
        string? assetName = e.Asset != null ? $"{e.Asset.AssetNumber} - {e.Asset.Name}" : null;

        return new ComplianceExceptionDto(
            e.Id,
            e.TenantId,
            e.ComplianceRequirementId,
            e.ComplianceRequirement?.Code ?? string.Empty,
            e.ComplianceRequirement?.Name ?? string.Empty,
            e.SubjectType,
            e.VehicleId,
            vehicleName,
            e.DriverId,
            driverName,
            e.AssetId,
            assetName,
            e.Reason,
            e.ApprovedByUserId,
            null,
            e.EffectiveFromUtc,
            e.EffectiveToUtc,
            e.Status,
            e.IsCurrentlyActive(),
            e.CreatedAtUtc);
    }
}
