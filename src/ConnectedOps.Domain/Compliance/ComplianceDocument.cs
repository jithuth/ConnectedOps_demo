using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Compliance;

public sealed class ComplianceDocument : BaseEntity
{
    private ComplianceDocument()
    {
    }

    public ComplianceDocument(
        Guid tenantId,
        Guid complianceRecordId,
        string documentType,
        string title,
        string fileObjectKey,
        string fileName,
        string contentType,
        long fileSizeBytes,
        DateTime? issueDateUtc = null,
        DateTime? expiryDateUtc = null,
        DateTime? uploadedAtUtc = null,
        Guid? uploadedByUserId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (complianceRecordId == Guid.Empty)
            throw new ArgumentException("ComplianceRecordId is required.", nameof(complianceRecordId));
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Document type is required.", nameof(documentType));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(fileObjectKey))
            throw new ArgumentException("FileObjectKey is required.", nameof(fileObjectKey));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));

        TenantId = tenantId;
        ComplianceRecordId = complianceRecordId;
        DocumentType = documentType.Trim();
        Title = title.Trim();
        FileObjectKey = fileObjectKey.Trim();
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        IssueDateUtc = issueDateUtc;
        ExpiryDateUtc = expiryDateUtc;
        UploadedAtUtc = uploadedAtUtc ?? DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
        Notes = notes?.Trim();
        CreatedBy = uploadedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid ComplianceRecordId { get; private set; }
    public ComplianceRecord ComplianceRecord { get; private set; } = null!;

    public string DocumentType { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string FileObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long FileSizeBytes { get; private set; }
    public DateTime? IssueDateUtc { get; private set; }
    public DateTime? ExpiryDateUtc { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public string? Notes { get; private set; }
}
