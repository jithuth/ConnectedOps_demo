using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetInspectionItem : BaseEntity
{
    private AssetInspectionItem()
    {
    }

    public AssetInspectionItem(
        Guid tenantId,
        Guid assetInspectionId,
        string name,
        string? description = null,
        AssetInspectionResult result = AssetInspectionResult.Passed,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetInspectionId == Guid.Empty)
            throw new ArgumentException("AssetInspectionId is required.", nameof(assetInspectionId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name is required.", nameof(name));

        TenantId = tenantId;
        AssetInspectionId = assetInspectionId;
        Name = name.Trim();
        Description = description?.Trim();
        Result = result;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid AssetInspectionId { get; private set; }
    public AssetInspection AssetInspection { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public AssetInspectionResult Result { get; private set; }
    public string? Notes { get; private set; }

    public void Update(string name, string? description, AssetInspectionResult result, string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        Result = result;
        Notes = notes?.Trim();
        MarkUpdated();
    }
}
