using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetTypeService : IAssetTypeService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetTypeService(
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

    public async Task<IReadOnlyList<AssetTypeDto>> GetAllAsync(Guid? categoryId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.AssetTypes
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .Where(x => x.TenantId == tenantId);

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.AssetCategoryId == categoryId.Value);
        }

        var types = await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);

        var assetCounts = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AssetTypeId.HasValue)
            .GroupBy(x => x.AssetTypeId!.Value)
            .Select(g => new { TypeId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TypeId, x => x.Count, cancellationToken);

        return types.Select(x => new AssetTypeDto(
            x.Id,
            x.AssetCategoryId,
            x.AssetCategory?.Name,
            x.Code,
            x.Name,
            x.Description,
            x.RequiresInspection,
            null,
            x.RequiresCalibration,
            null,
            false,
            x.IsActive,
            assetCounts.GetValueOrDefault(x.Id, 0),
            x.CreatedAtUtc)).ToList();
    }

    public async Task<AssetTypeDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var item = await _dbContext.AssetTypes
            .AsNoTracking()
            .Include(x => x.AssetCategory)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;

        var assetCount = await _dbContext.Assets
            .CountAsync(x => x.TenantId == tenantId && x.AssetTypeId == id, cancellationToken);

        return new AssetTypeDto(
            item.Id,
            item.AssetCategoryId,
            item.AssetCategory?.Name,
            item.Code,
            item.Name,
            item.Description,
            item.RequiresInspection,
            null,
            item.RequiresCalibration,
            null,
            false,
            item.IsActive,
            assetCount,
            item.CreatedAtUtc);
    }

    public async Task<AssetTypeDto> CreateAsync(CreateAssetTypeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var categoryExists = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new ValidationException("Asset category does not exist.");

        var exists = await _dbContext.AssetTypes
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);
        if (exists)
            throw new ConflictException($"Asset type with code '{code}' already exists.");

        var assetType = new AssetType(
            tenantId,
            request.CategoryId,
            code,
            request.Name.Trim(),
            request.Description?.Trim(),
            requiresSerialNumber: false,
            requiresInspection: request.RequiresInspection,
            requiresCalibration: request.RequiresCalibration,
            requiresWarrantyTracking: false,
            isActive: request.IsActive);

        _dbContext.AssetTypes.Add(assetType);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTypeCreated,
            nameof(AssetType),
            assetType.Id.ToString(),
            $"Created asset type '{assetType.Name}' ({assetType.Code})",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(assetType.Id, cancellationToken))!;
    }

    public async Task<AssetTypeDto> UpdateAsync(Guid id, UpdateAssetTypeRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var assetType = await _dbContext.AssetTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (assetType is null)
            throw new KeyNotFoundException($"Asset type '{id}' was not found.");

        var categoryExists = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
            throw new ValidationException("Asset category does not exist.");

        var codeInUse = await _dbContext.AssetTypes
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, cancellationToken);
        if (codeInUse)
            throw new ConflictException($"Asset type with code '{code}' already exists.");

        assetType.Update(
            request.CategoryId,
            code,
            request.Name.Trim(),
            request.Description?.Trim(),
            requiresSerialNumber: false,
            requiresInspection: request.RequiresInspection,
            requiresCalibration: request.RequiresCalibration,
            requiresWarrantyTracking: false,
            isActive: request.IsActive,
            updatedBy: _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTypeUpdated,
            nameof(AssetType),
            assetType.Id.ToString(),
            $"Updated asset type '{assetType.Name}' ({assetType.Code})",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(assetType.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var assetType = await _dbContext.AssetTypes
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (assetType is null)
            throw new KeyNotFoundException($"Asset type '{id}' was not found.");

        var hasAssets = await _dbContext.Assets
            .AnyAsync(x => x.TenantId == tenantId && x.AssetTypeId == id, cancellationToken);
        if (hasAssets)
            throw new ValidationException("Cannot delete asset type with associated assets.");

        _dbContext.AssetTypes.Remove(assetType);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTypeDeleted,
            nameof(AssetType),
            id.ToString(),
            $"Deleted asset type '{assetType.Name}' ({assetType.Code})",
            null,
            null), cancellationToken);
    }
}
