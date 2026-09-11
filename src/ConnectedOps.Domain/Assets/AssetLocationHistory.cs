using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetLocationHistory : BaseEntity
{
    private AssetLocationHistory()
    {
    }

    public AssetLocationHistory(
        Guid tenantId,
        Guid assetId,
        Guid? branchId,
        Guid? locationId,
        DateTime effectiveFromUtc,
        DateTime? effectiveToUtc = null,
        string? reason = null,
        Guid? changedByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        BranchId = branchId;
        LocationId = locationId;
        EffectiveFromUtc = effectiveFromUtc;
        EffectiveToUtc = effectiveToUtc;
        Reason = reason?.Trim();
        ChangedByUserId = changedByUserId;
        CreatedBy = changedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }
    public DateTime EffectiveFromUtc { get; private set; }
    public DateTime? EffectiveToUtc { get; private set; }
    public string? Reason { get; private set; }
    public Guid? ChangedByUserId { get; private set; }

    public void EndAssignment(DateTime effectiveToUtc)
    {
        EffectiveToUtc = effectiveToUtc;
        MarkUpdated();
    }
}
