using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverLicense : BaseEntity
{
    private readonly List<DriverLicenseCategory> _categories = [];

    private DriverLicense()
    {
    }

    public DriverLicense(
        Guid tenantId,
        Guid driverId,
        string licenseNumber,
        string licenseCountryCode,
        string? issuingAuthority = null,
        DateOnly? issueDate = null,
        DateOnly? expiryDate = null,
        bool isPrimary = false,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverId == Guid.Empty)
            throw new ArgumentException("DriverId is required.", nameof(driverId));
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new ArgumentException("License number is required.", nameof(licenseNumber));
        if (string.IsNullOrWhiteSpace(licenseCountryCode))
            throw new ArgumentException("License country code is required.", nameof(licenseCountryCode));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("License expiry date cannot be earlier than issue date.");

        TenantId = tenantId;
        DriverId = driverId;
        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
        LicenseCountryCode = licenseCountryCode.Trim().ToUpperInvariant();
        IssuingAuthority = issuingAuthority?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        IsPrimary = isPrimary;
        IsActive = true;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid DriverId { get; private set; }
    public Driver Driver { get; private set; } = null!;
    public string LicenseNumber { get; private set; } = string.Empty;
    public string LicenseCountryCode { get; private set; } = string.Empty;
    public string? IssuingAuthority { get; private set; }
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<DriverLicenseCategory> Categories => _categories;

    public void Update(
        string licenseNumber,
        string licenseCountryCode,
        string? issuingAuthority,
        DateOnly? issueDate,
        DateOnly? expiryDate,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(licenseNumber))
            throw new ArgumentException("License number is required.", nameof(licenseNumber));
        if (string.IsNullOrWhiteSpace(licenseCountryCode))
            throw new ArgumentException("License country code is required.", nameof(licenseCountryCode));

        if (issueDate.HasValue && expiryDate.HasValue && expiryDate < issueDate)
            throw new ArgumentException("License expiry date cannot be earlier than issue date.");

        LicenseNumber = licenseNumber.Trim().ToUpperInvariant();
        LicenseCountryCode = licenseCountryCode.Trim().ToUpperInvariant();
        IssuingAuthority = issuingAuthority?.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
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
        IsPrimary = false;
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
