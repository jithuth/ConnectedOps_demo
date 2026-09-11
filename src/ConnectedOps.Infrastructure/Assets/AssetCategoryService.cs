using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetCategoryService : IAssetCategoryService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetCategoryService(
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

    public async Task<IReadOnlyList<AssetCategoryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var categories = await _dbContext.AssetCategories
            .AsNoTracking()
            .Include(x => x.ParentCategory)
            .Include(x => x.SubCategories)
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var assetCounts = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.AssetCategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        return categories.Select(x => new AssetCategoryDto(
            x.Id,
            x.Code,
            x.Name,
            x.Description,
            x.ParentCategoryId,
            x.ParentCategory?.Name,
            x.IsActive,
            assetCounts.GetValueOrDefault(x.Id, 0),
            x.SubCategories.Count,
            x.CreatedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<AssetCategoryTreeDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var allCategories = await _dbContext.AssetCategories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var assetCounts = await _dbContext.Assets
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .GroupBy(x => x.AssetCategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        var lookup = allCategories.ToLookup(x => x.ParentCategoryId);

        List<AssetCategoryTreeDto> BuildTree(Guid? parentId)
        {
            return lookup[parentId].Select(cat => new AssetCategoryTreeDto(
                cat.Id,
                cat.Code,
                cat.Name,
                cat.Description,
                cat.ParentCategoryId,
                cat.IsActive,
                assetCounts.GetValueOrDefault(cat.Id, 0),
                BuildTree(cat.Id))).ToList();
        }

        return BuildTree(null);
    }

    public async Task<AssetCategoryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var item = await _dbContext.AssetCategories
            .AsNoTracking()
            .Include(x => x.ParentCategory)
            .Include(x => x.SubCategories)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;

        var assetCount = await _dbContext.Assets
            .CountAsync(x => x.TenantId == tenantId && x.AssetCategoryId == id, cancellationToken);

        return new AssetCategoryDto(
            item.Id,
            item.Code,
            item.Name,
            item.Description,
            item.ParentCategoryId,
            item.ParentCategory?.Name,
            item.IsActive,
            assetCount,
            item.SubCategories.Count,
            item.CreatedAtUtc);
    }

    public async Task<AssetCategoryDto> CreateAsync(CreateAssetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code, cancellationToken);
        if (exists)
            throw new ConflictException($"Asset category with code '{code}' already exists.");

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _dbContext.AssetCategories
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.ParentCategoryId.Value, cancellationToken);
            if (!parentExists)
                throw new ValidationException("Parent category does not exist.");
        }

        var category = new AssetCategory(
            tenantId,
            code,
            request.Name.Trim(),
            request.Description?.Trim(),
            request.ParentCategoryId,
            request.IsActive);

        _dbContext.AssetCategories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCategoryCreated,
            nameof(AssetCategory),
            category.Id.ToString(),
            $"Created asset category '{category.Name}' ({category.Code})",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(category.Id, cancellationToken))!;
    }

    public async Task<AssetCategoryDto> UpdateAsync(Guid id, UpdateAssetCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var code = request.Code.Trim().ToUpperInvariant();

        var category = await _dbContext.AssetCategories
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Asset category '{id}' was not found.");

        var codeInUse = await _dbContext.AssetCategories
            .AnyAsync(x => x.TenantId == tenantId && x.Code == code && x.Id != id, cancellationToken);
        if (codeInUse)
            throw new ConflictException($"Asset category with code '{code}' already exists.");

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == id)
                throw new ValidationException("A category cannot be its own parent.");

            var allCategories = await _dbContext.AssetCategories
                .Where(x => x.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            if (!allCategories.Any(x => x.Id == request.ParentCategoryId.Value))
                throw new ValidationException("Parent category does not exist.");

            var curr = request.ParentCategoryId.Value;
            while (curr != Guid.Empty)
            {
                if (curr == id)
                    throw new ValidationException("Circular category hierarchy is not allowed.");

                var parent = allCategories.FirstOrDefault(x => x.Id == curr);
                if (parent?.ParentCategoryId is null)
                    break;
                curr = parent.ParentCategoryId.Value;
            }
        }

        category.Update(
            code,
            request.Name.Trim(),
            request.Description?.Trim(),
            request.ParentCategoryId,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCategoryUpdated,
            nameof(AssetCategory),
            category.Id.ToString(),
            $"Updated asset category '{category.Name}' ({category.Code})",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(category.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var category = await _dbContext.AssetCategories
            .Include(x => x.SubCategories)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Asset category '{id}' was not found.");

        if (category.SubCategories.Count != 0)
            throw new ValidationException("Cannot delete category with subcategories. Remove subcategories first.");

        var hasAssets = await _dbContext.Assets
            .AnyAsync(x => x.TenantId == tenantId && x.AssetCategoryId == id, cancellationToken);
        if (hasAssets)
            throw new ValidationException("Cannot delete category with associated assets.");

        var hasTypes = await _dbContext.AssetTypes
            .AnyAsync(x => x.TenantId == tenantId && x.AssetCategoryId == id, cancellationToken);
        if (hasTypes)
            throw new ValidationException("Cannot delete category with associated asset types.");

        _dbContext.AssetCategories.Remove(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetCategoryDeleted,
            nameof(AssetCategory),
            id.ToString(),
            $"Deleted asset category '{category.Name}' ({category.Code})",
            null,
            null), cancellationToken);
    }
}
