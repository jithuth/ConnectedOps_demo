using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetCategory : BaseEntity
{
    private readonly List<AssetCategory> _subCategories = [];

    private AssetCategory()
    {
    }

    public AssetCategory(
        Guid tenantId,
        string code,
        string name,
        string? description = null,
        Guid? parentCategoryId = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Category code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        ParentCategoryId = parentCategoryId;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public AssetCategory? ParentCategory { get; private set; }
    public IReadOnlyCollection<AssetCategory> SubCategories => _subCategories.AsReadOnly();
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        string? description,
        Guid? parentCategoryId,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Category code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));
        if (parentCategoryId.HasValue && parentCategoryId.Value == Id)
            throw new ArgumentException("Category cannot be its own parent.");

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        ParentCategoryId = parentCategoryId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
