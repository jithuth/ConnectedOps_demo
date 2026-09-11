using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Maintenance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Maintenance;

public sealed class MaintenanceServiceTypeService : IMaintenanceServiceTypeService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public MaintenanceServiceTypeService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<MaintenanceServiceTypeDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.MaintenanceServiceTypes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var types = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return types.Select(MapToDto).ToList();
    }

    public async Task<MaintenanceServiceTypeDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.MaintenanceServiceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Maintenance service type '{id}' was not found.");

        return MapToDto(item);
    }

    public async Task<MaintenanceServiceTypeDto> CreateAsync(
        CreateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.MaintenanceServiceTypes
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance service type with code '{normalizedCode}' already exists for this tenant.");

        var serviceType = new MaintenanceServiceType(
            tenantId,
            normalizedCode,
            request.Name,
            request.Category,
            request.Description,
            request.DefaultDurationHours,
            request.IsActive);

        _dbContext.MaintenanceServiceTypes.Add(serviceType);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceServiceTypeCreated,
                "MaintenanceServiceType",
                serviceType.Id.ToString(),
                $"Created maintenance service type '{serviceType.Name}' ({serviceType.Code})"),
            cancellationToken);

        return MapToDto(serviceType);
    }

    public async Task<MaintenanceServiceTypeDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (serviceType is null)
            throw new KeyNotFoundException($"Maintenance service type '{id}' was not found.");

        var codeExists = await _dbContext.MaintenanceServiceTypes
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Maintenance service type with code '{normalizedCode}' already exists for this tenant.");

        serviceType.Update(
            normalizedCode,
            request.Name,
            request.Category,
            request.Description,
            request.DefaultDurationHours,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceServiceTypeUpdated,
                "MaintenanceServiceType",
                serviceType.Id.ToString(),
                $"Updated maintenance service type '{serviceType.Name}' ({serviceType.Code})"),
            cancellationToken);

        return MapToDto(serviceType);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var serviceType = await _dbContext.MaintenanceServiceTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (serviceType is null)
            throw new KeyNotFoundException($"Maintenance service type '{id}' was not found.");

        // Check if referenced in completed/active records or plan rules
        var isUsedInRecords = await _dbContext.VehicleMaintenanceRecords
            .AnyAsync(x => x.TenantId == tenantId && x.MaintenanceServiceTypeId == id, cancellationToken);

        var isUsedInRules = await _dbContext.MaintenancePlanRules
            .AnyAsync(x => x.TenantId == tenantId && x.MaintenanceServiceTypeId == id, cancellationToken);

        if (isUsedInRecords || isUsedInRules)
        {
            // Soft delete / deactivate to preserve historical integrity
            serviceType.SoftDelete(_currentUserContext.UserId);
        }
        else
        {
            serviceType.SoftDelete(_currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.MaintenanceServiceTypeDeleted,
                "MaintenanceServiceType",
                serviceType.Id.ToString(),
                $"Deleted maintenance service type '{serviceType.Name}' ({serviceType.Code})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static MaintenanceServiceTypeDto MapToDto(MaintenanceServiceType entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.Category,
            entity.Category.ToString(),
            entity.Description,
            entity.DefaultDurationHours,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}
