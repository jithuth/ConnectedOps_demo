using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Drivers;

public sealed class DriverLicenseCategory : BaseEntity
{
    private DriverLicenseCategory()
    {
    }

    public DriverLicenseCategory(
        Guid tenantId,
        Guid driverLicenseId,
        string categoryCode,
        string? description = null,
        DateOnly? validFrom = null,
        DateOnly? validTo = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (driverLicenseId == Guid.Empty)
            throw new ArgumentException("DriverLicenseId is required.", nameof(driverLicenseId));
        if (string.IsNullOrWhiteSpace(categoryCode))
            throw new ArgumentException("Category code is required.", nameof(categoryCode));

        if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
            throw new ArgumentException("ValidTo date cannot be earlier than ValidFrom date.");

        TenantId = tenantId;
        DriverLicenseId = driverLicenseId;
        CategoryCode = categoryCode.Trim().ToUpperInvariant();
        Description = description?.Trim();
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid DriverLicenseId { get; private set; }
    public DriverLicense DriverLicense { get; private set; } = null!;
    public string CategoryCode { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string? description, DateOnly? validFrom, DateOnly? validTo)
    {
        if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
            throw new ArgumentException("ValidTo date cannot be earlier than ValidFrom date.");

        Description = description?.Trim();
        ValidFrom = validFrom;
        ValidTo = validTo;
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
}
