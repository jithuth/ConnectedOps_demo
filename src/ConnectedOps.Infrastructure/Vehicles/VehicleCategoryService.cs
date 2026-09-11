using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleCategoryService : IVehicleCategoryService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public VehicleCategoryService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<VehicleCategoryDto>> GetCategoriesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.VehicleCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var categories = await query
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                Category = x,
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleCategoryId == x.Id)
            })
            .ToListAsync(cancellationToken);

        return categories.Select(x => new VehicleCategoryDto(
            x.Category.Id,
            x.Category.TenantId,
            x.Category.Name,
            x.Category.Code,
            x.Category.Description,
            x.Category.IsMotorized,
            x.Category.IsActive,
            x.VehicleCount)).ToList();
    }

    public async Task<VehicleCategoryDto> GetCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.VehicleCategories
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId)
            .Select(x => new
            {
                Category = x,
                VehicleCount = _dbContext.Vehicles.Count(v => v.TenantId == tenantId && v.VehicleCategoryId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Vehicle category '{id}' was not found.");

        return new VehicleCategoryDto(
            item.Category.Id,
            item.Category.TenantId,
            item.Category.Name,
            item.Category.Code,
            item.Category.Description,
            item.Category.IsMotorized,
            item.Category.IsActive,
            item.VehicleCount);
    }

    public async Task<VehicleCategoryDto> CreateCategoryAsync(
        CreateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var codeExists = await _dbContext.VehicleCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Category code '{normalizedCode}' already exists.");

        var category = new VehicleCategory(
            tenantId,
            request.Name,
            normalizedCode,
            request.Description,
            request.IsMotorized);

        _dbContext.VehicleCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "VehicleCategory",
                category.Id.ToString(),
                $"Created vehicle category: {category.Name} ({category.Code})"),
            cancellationToken);

        return new VehicleCategoryDto(
            category.Id,
            category.TenantId,
            category.Name,
            category.Code,
            category.Description,
            category.IsMotorized,
            category.IsActive,
            0);
    }

    public async Task<VehicleCategoryDto> UpdateCategoryAsync(
        Guid id,
        UpdateVehicleCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Vehicle category '{id}' was not found.");

        category.Update(request.Name, request.Description, request.IsMotorized);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "VehicleCategory",
                category.Id.ToString(),
                $"Updated vehicle category: {category.Name}"),
            cancellationToken);

        var vehicleCount = await _dbContext.Vehicles
            .CountAsync(v => v.TenantId == tenantId && v.VehicleCategoryId == category.Id, cancellationToken);

        return new VehicleCategoryDto(
            category.Id,
            category.TenantId,
            category.Name,
            category.Code,
            category.Description,
            category.IsMotorized,
            category.IsActive,
            vehicleCount);
    }

    public async Task ActivateCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Vehicle category '{id}' was not found.");

        category.Activate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Activated,
                "VehicleCategory",
                category.Id.ToString(),
                $"Activated vehicle category: {category.Name}"),
            cancellationToken);
    }

    public async Task DeactivateCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Vehicle category '{id}' was not found.");

        category.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deactivated,
                "VehicleCategory",
                category.Id.ToString(),
                $"Deactivated vehicle category: {category.Name}"),
            cancellationToken);
    }

    public async Task DeleteCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var category = await _dbContext.VehicleCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Vehicle category '{id}' was not found.");

        var hasVehicles = await _dbContext.Vehicles
            .AnyAsync(v => v.TenantId == tenantId && v.VehicleCategoryId == id, cancellationToken);

        if (hasVehicles)
            throw new ConflictException("Cannot delete category because vehicles are assigned to it.");

        category.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "VehicleCategory",
                category.Id.ToString(),
                $"Deleted vehicle category: {category.Name}"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
