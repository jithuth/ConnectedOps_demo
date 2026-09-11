using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverDocument : BaseEntity
{
    private DriverDocument()
    {
    }

    public DriverDocument(
        Guid tenantId,
        Guid driverId,
        DriverDocumentType documentType,
        string title,
        string? documentNumber = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        string? issuingAuthority = null,
        string? fileObjectKey = null,
        string? fileName = null,
        string? contentType = null,
        long? fileSizeBytes = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("Document expiry date cannot be earlier than issue date.");

        TenantId = tenantId;
        DriverId = driverId;
        DocumentType = documentType;
        Title = title.Trim();
        DocumentNumber = documentNumber?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        IssuingAuthority = issuingAuthority?.Trim();
        FileObjectKey = fileObjectKey?.Trim();
        FileName = fileName?.Trim();
        ContentType = contentType?.Trim();
        FileSizeBytes = fileSizeBytes;
        Notes = notes?.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public DriverDocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? DocumentNumber { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? IssuingAuthority { get; private set; }
    public string? FileObjectKey { get; private set; }
    public string? FileName { get; private set; }
    public string? ContentType { get; private set; }
    public long? FileSizeBytes { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        DriverDocumentType documentType,
        string title,
        string? documentNumber,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        string? issuingAuthority,
        string? fileObjectKey,
        string? fileName,
        string? contentType,
        long? fileSizeBytes,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("Document expiry date cannot be earlier than issue date.");

        DocumentType = documentType;
        Title = title.Trim();
        DocumentNumber = documentNumber?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        IssuingAuthority = issuingAuthority?.Trim();
        if (!string.IsNullOrWhiteSpace(fileObjectKey))
        {
            FileObjectKey = fileObjectKey.Trim();
            FileName = fileName?.Trim();
            ContentType = contentType?.Trim();
            FileSizeBytes = fileSizeBytes;
        }
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public bool IsExpired()
    {
        if (!ExpiryDate.HasValue) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpiryDate.Value < today;
    }

    public bool IsExpiringSoon(int days = 30)
    {
        if (!ExpiryDate.HasValue) return false;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpiryDate.Value >= today && ExpiryDate.Value <= today.AddDays(days);
    }
}
