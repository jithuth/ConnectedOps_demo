using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetNote : BaseEntity
{
    private AssetNote()
    {
    }

    public AssetNote(
        Guid tenantId,
        Guid assetId,
        string noteText,
        Guid? createdByUserId = null,
        string? createdByUserName = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (string.IsNullOrWhiteSpace(noteText))
            throw new ArgumentException("Note text is required.", nameof(noteText));

        TenantId = tenantId;
        AssetId = assetId;
        NoteText = noteText.Trim();
        CreatedByUserId = createdByUserId;
        CreatedByUserName = createdByUserName?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public string NoteText { get; private set; } = string.Empty;
    public Guid? CreatedByUserId { get; private set; }
    public string? CreatedByUserName { get; private set; }
}
