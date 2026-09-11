using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetIdentifier : BaseEntity
{
    private AssetIdentifier()
    {
    }

    public AssetIdentifier(
        Guid tenantId,
        Guid assetId,
        AssetIdentifierType identifierType,
        string value,
        string? publicToken = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Identifier value is required.", nameof(value));

        TenantId = tenantId;
        AssetId = assetId;
        IdentifierType = identifierType;
        Value = value.Trim();
        PublicToken = string.IsNullOrWhiteSpace(publicToken) ? Guid.NewGuid().ToString("N") : publicToken.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public AssetIdentifierType IdentifierType { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string PublicToken { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        MarkUpdated(userId);
    }

    public void RegenerateToken(Guid? userId = null)
    {
        PublicToken = Guid.NewGuid().ToString("N");
        IsActive = true;
        MarkUpdated(userId);
    }
}
