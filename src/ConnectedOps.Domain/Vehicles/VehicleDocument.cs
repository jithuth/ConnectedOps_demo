using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleDocument : BaseEntity
{
    private VehicleDocument()
    {
    }

    public VehicleDocument(
        Guid tenantId,
        Guid vehicleId,
        VehicleDocumentType documentType,
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
        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId is required.", nameof(vehicleId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Document title is required.", nameof(title));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("Document expiry date cannot be earlier than issue date.");

        TenantId = tenantId;
        VehicleId = vehicleId;
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
    public Guid VehicleId { get; private set; }
    public Vehicle Vehicle { get; private set; } = null!;

    public VehicleDocumentType DocumentType { get; private set; }
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

    public bool IsExpired(DateOnly? asOfDate = null)
    {
        if (!ExpiryDate.HasValue)
            return false;

        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpiryDate.Value < referenceDate;
    }

    public bool IsExpiringSoon(int daysThreshold = 30, DateOnly? asOfDate = null)
    {
        if (!ExpiryDate.HasValue)
            return false;

        var referenceDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        return ExpiryDate.Value >= referenceDate && ExpiryDate.Value <= referenceDate.AddDays(daysThreshold);
    }

    public void Update(
        VehicleDocumentType documentType,
        string title,
        string? documentNumber,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        string? issuingAuthority,
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
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
