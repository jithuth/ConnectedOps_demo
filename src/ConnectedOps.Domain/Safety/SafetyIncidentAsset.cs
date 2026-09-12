using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncidentAsset : BaseEntity
{
    private SafetyIncidentAsset()
    {
    }

    public SafetyIncidentAsset(
        Guid tenantId,
        Guid safetyIncidentId,
        Guid assetId,
        bool damageReported = false,
        string? damageDescription = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (safetyIncidentId == Guid.Empty)
            throw new ArgumentException("SafetyIncidentId is required.", nameof(safetyIncidentId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        SafetyIncidentId = safetyIncidentId;
        AssetId = assetId;
        DamageReported = damageReported;
        DamageDescription = damageDescription?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid SafetyIncidentId { get; private set; }
    public SafetyIncident SafetyIncident { get; private set; } = null!;

    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;

    public bool DamageReported { get; private set; }
    public string? DamageDescription { get; private set; }

    public void Update(
        bool damageReported,
        string? damageDescription,
        Guid? updatedByUserId = null)
    {
        DamageReported = damageReported;
        DamageDescription = damageDescription?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }
}
