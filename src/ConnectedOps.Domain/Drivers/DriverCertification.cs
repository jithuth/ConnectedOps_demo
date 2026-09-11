using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverCertification : BaseEntity
{
    private DriverCertification()
    {
    }

    public DriverCertification(
        Guid tenantId,
        Guid driverId,
        CertificationType certificationType,
        string title,
        string? certificateNumber = null,
        string? issuedBy = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        string? fileObjectKey = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Certification title is required.", nameof(title));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("Certification expiry date cannot be earlier than issue date.");

        TenantId = tenantId;
        DriverId = driverId;
        CertificationType = certificationType;
        Title = title.Trim();
        CertificateNumber = certificateNumber?.Trim();
        IssuedBy = issuedBy?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        FileObjectKey = fileObjectKey?.Trim();
        Notes = notes?.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public CertificationType CertificationType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? CertificateNumber { get; private set; }
    public string? IssuedBy { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string? FileObjectKey { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        CertificationType certificationType,
        string title,
        string? certificateNumber,
        string? issuedBy,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        string? fileObjectKey,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Certification title is required.", nameof(title));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("Certification expiry date cannot be earlier than issue date.");

        CertificationType = certificationType;
        Title = title.Trim();
        CertificateNumber = certificateNumber?.Trim();
        IssuedBy = issuedBy?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        if (!string.IsNullOrWhiteSpace(fileObjectKey))
        {
            FileObjectKey = fileObjectKey.Trim();
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
