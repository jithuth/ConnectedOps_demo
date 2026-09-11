using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetType : BaseEntity
{
    private AssetType()
    {
    }

    public AssetType(
        Guid tenantId,
        Guid assetCategoryId,
        string code,
        string name,
        string? description = null,
        bool requiresSerialNumber = false,
        bool requiresInspection = false,
        bool requiresCalibration = false,
        bool requiresWarrantyTracking = false,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetCategoryId == Guid.Empty)
            throw new ArgumentException("AssetCategoryId is required.", nameof(assetCategoryId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Asset type code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset type name is required.", nameof(name));

        TenantId = tenantId;
        AssetCategoryId = assetCategoryId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        RequiresSerialNumber = requiresSerialNumber;
        RequiresInspection = requiresInspection;
        RequiresCalibration = requiresCalibration;
        RequiresWarrantyTracking = requiresWarrantyTracking;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetCategoryId { get; private set; }
    public AssetCategory AssetCategory { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool RequiresSerialNumber { get; private set; }
    public bool RequiresInspection { get; private set; }
    public bool RequiresCalibration { get; private set; }
    public bool RequiresWarrantyTracking { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        Guid assetCategoryId,
        string code,
        string name,
        string? description,
        bool requiresSerialNumber,
        bool requiresInspection,
        bool requiresCalibration,
        bool requiresWarrantyTracking,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (assetCategoryId == Guid.Empty)
            throw new ArgumentException("AssetCategoryId is required.", nameof(assetCategoryId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Asset type code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset type name is required.", nameof(name));

        AssetCategoryId = assetCategoryId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        RequiresSerialNumber = requiresSerialNumber;
        RequiresInspection = requiresInspection;
        RequiresCalibration = requiresCalibration;
        RequiresWarrantyTracking = requiresWarrantyTracking;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
