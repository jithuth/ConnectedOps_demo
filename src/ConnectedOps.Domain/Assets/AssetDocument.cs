using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetDocument : BaseEntity
{
    private AssetDocument()
    {
    }

    public AssetDocument(
        Guid tenantId,
        Guid assetId,
        AssetDocumentType documentType,
        string title,
        string fileObjectKey,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string? documentNumber = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        string? notes = null,
        bool isActive = true,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(fileObjectKey))
            throw new ArgumentException("FileObjectKey is required.", nameof(fileObjectKey));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));

        TenantId = tenantId;
        AssetId = assetId;
        DocumentType = documentType;
        Title = title.Trim();
        FileObjectKey = fileObjectKey.Trim();
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        DocumentNumber = documentNumber?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Notes = notes?.Trim();
        IsActive = isActive;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public AssetDocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? DocumentNumber { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string FileObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow);
    public bool IsExpiringSoon => ExpiryDate.HasValue && !IsExpired && ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30));

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        MarkUpdated(userId);
    }
}
