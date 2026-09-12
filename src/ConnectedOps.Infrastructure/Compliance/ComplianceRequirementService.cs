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

public sealed class ComplianceRequirementService : IComplianceRequirementService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public ComplianceRequirementService(
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

    public async Task<PagedResult<ComplianceRequirementDto>> GetPagedAsync(ComplianceRequirementFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.ComplianceRequirements
            .AsNoTracking()
            .Include(x => x.Rules)
                .ThenInclude(r => r.VehicleCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.AssetCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.Branch)
            .Where(x => x.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term) || (x.Description != null && x.Description.ToLower().Contains(term)));
        }

        if (filter.AppliesTo.HasValue)
        {
            query = query.Where(x => x.AppliesTo == filter.AppliesTo.Value);
        }

        if (filter.RequirementType.HasValue)
        {
            query = query.Where(x => x.RequirementType == filter.RequirementType.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        if (filter.IsMandatory.HasValue)
        {
            query = query.Where(x => x.IsMandatory == filter.IsMandatory.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<ComplianceRequirementDto>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<ComplianceRequirementDto>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var items = await _dbContext.ComplianceRequirements
            .AsNoTracking()
            .Include(x => x.Rules)
                .ThenInclude(r => r.VehicleCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.AssetCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.Branch)
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return items.Select(x => MapToDto(x)).ToList();
    }

    public async Task<ComplianceRequirementDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRequirements
            .AsNoTracking()
            .Include(x => x.Rules)
                .ThenInclude(r => r.VehicleCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.AssetCategory)
            .Include(x => x.Rules)
                .ThenInclude(r => r.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<ComplianceRequirementDto> CreateAsync(CreateComplianceRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.ComplianceRequirements
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);

        if (exists)
            throw new ConflictException($"Compliance requirement with code '{code}' already exists.");

        var entity = new ComplianceRequirement(
            tenantId,
            code,
            request.Name,
            request.AppliesTo,
            request.RequirementType,
            request.ValidityType,
            request.Description,
            request.DefaultValidityDays,
            request.DefaultReminderDays,
            request.IsMandatory,
            request.IsActive,
            _currentUserContext.UserId);

        _dbContext.ComplianceRequirements.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRequirementCreated,
            nameof(ComplianceRequirement),
            entity.Id.ToString(),
            $"Created compliance requirement {entity.Code} ({entity.Name})",
            null,
            entity));

        return MapToDto(entity);
    }

    public async Task<ComplianceRequirementDto> UpdateAsync(Guid id, UpdateComplianceRequirementRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRequirements
            .Include(x => x.Rules)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceRequirement), id.ToString());

        entity.Update(
            request.Name,
            request.Description,
            request.AppliesTo,
            request.RequirementType,
            request.ValidityType,
            request.DefaultValidityDays,
            request.DefaultReminderDays,
            request.IsMandatory,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRequirementUpdated,
            nameof(ComplianceRequirement),
            entity.Id.ToString(),
            $"Updated compliance requirement {entity.Code} ({entity.Name})",
            null,
            entity));

        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRequirements
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceRequirement), id.ToString());

        var hasRecords = await _dbContext.ComplianceRecords
            .AnyAsync(x => x.ComplianceRequirementId == id && x.TenantId == tenantId, cancellationToken);

        if (hasRecords)
            throw new InvalidOperationException("Cannot delete compliance requirement that is referenced by compliance records.");

        entity.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRequirementDeleted,
            nameof(ComplianceRequirement),
            entity.Id.ToString(),
            $"Deleted compliance requirement {entity.Code} ({entity.Name})"));
    }

    public async Task<ComplianceRequirementRuleDto> AddRuleAsync(CreateComplianceRequirementRuleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var requirement = await _dbContext.ComplianceRequirements
            .FirstOrDefaultAsync(x => x.Id == request.ComplianceRequirementId && x.TenantId == tenantId, cancellationToken);

        if (requirement == null)
            throw new NotFoundException(nameof(ComplianceRequirement), request.ComplianceRequirementId.ToString());

        var rule = new ComplianceRequirementRule(
            tenantId,
            request.ComplianceRequirementId,
            request.VehicleCategoryId,
            request.AssetCategoryId,
            request.DriverType,
            request.CountryCode,
            request.BranchId,
            request.IsActive,
            request.Notes,
            _currentUserContext.UserId);

        _dbContext.ComplianceRequirementRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleDtoAsync(rule.Id, cancellationToken);
    }

    public async Task<ComplianceRequirementRuleDto> UpdateRuleAsync(Guid requirementId, Guid ruleId, UpdateComplianceRequirementRuleRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var rule = await _dbContext.ComplianceRequirementRules
            .FirstOrDefaultAsync(x => x.Id == ruleId && x.ComplianceRequirementId == requirementId && x.TenantId == tenantId, cancellationToken);

        if (rule == null)
            throw new NotFoundException(nameof(ComplianceRequirementRule), ruleId.ToString());

        rule.Update(
            request.VehicleCategoryId,
            request.AssetCategoryId,
            request.DriverType,
            request.CountryCode,
            request.BranchId,
            request.IsActive,
            request.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetRuleDtoAsync(rule.Id, cancellationToken);
    }

    public async Task DeleteRuleAsync(Guid requirementId, Guid ruleId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var rule = await _dbContext.ComplianceRequirementRules
            .FirstOrDefaultAsync(x => x.Id == ruleId && x.ComplianceRequirementId == requirementId && x.TenantId == tenantId, cancellationToken);

        if (rule == null)
            throw new NotFoundException(nameof(ComplianceRequirementRule), ruleId.ToString());

        _dbContext.ComplianceRequirementRules.Remove(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ComplianceRequirementRuleDto> GetRuleDtoAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.ComplianceRequirementRules
            .AsNoTracking()
            .Include(x => x.VehicleCategory)
            .Include(x => x.AssetCategory)
            .Include(x => x.Branch)
            .FirstAsync(x => x.Id == ruleId, cancellationToken);

        return MapRuleToDto(rule);
    }

    private static ComplianceRequirementDto MapToDto(ComplianceRequirement r) =>
        new(
            r.Id,
            r.TenantId,
            r.Code,
            r.Name,
            r.Description,
            r.AppliesTo,
            r.RequirementType,
            r.ValidityType,
            r.DefaultValidityDays,
            r.DefaultReminderDays,
            r.IsMandatory,
            r.IsActive,
            r.Rules.Count,
            r.CreatedAtUtc,
            r.Rules.Select(MapRuleToDto).ToList());

    private static ComplianceRequirementRuleDto MapRuleToDto(ComplianceRequirementRule r) =>
        new(
            r.Id,
            r.TenantId,
            r.ComplianceRequirementId,
            r.VehicleCategoryId,
            r.VehicleCategory?.Name,
            r.AssetCategoryId,
            r.AssetCategory?.Name,
            r.DriverType,
            r.CountryCode,
            r.BranchId,
            r.Branch?.Name,
            r.IsActive,
            r.Notes,
            r.CreatedAtUtc);
}
