using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class OrganizationProfile : BaseEntity
{
    private OrganizationProfile()
    {
    }

    public OrganizationProfile(
        Guid tenantId,
        string legalName,
        string? tradeName = null,
        string? registrationNumber = null,
        string? taxNumber = null,
        string? website = null,
        string? primaryContactEmail = null,
        string? primaryContactPhone = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? stateOrProvince = null,
        string? postalCode = null,
        string? countryCode = null,
        string currencyCode = "USD",
        string timeZoneId = "UTC")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetLegalName(legalName);
        UpdateDetails(
            tradeName,
            registrationNumber,
            taxNumber,
            website,
            primaryContactEmail,
            primaryContactPhone,
            addressLine1,
            addressLine2,
            city,
            stateOrProvince,
            postalCode,
            countryCode,
            currencyCode,
            timeZoneId);
    }

    public Guid TenantId { get; private set; }
    public string LegalName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? TaxNumber { get; private set; }
    public string? Website { get; private set; }
    public string? PrimaryContactEmail { get; private set; }
    public string? PrimaryContactPhone { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? StateOrProvince { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string TimeZoneId { get; private set; } = "UTC";

    public void SetLegalName(string legalName)
    {
        if (string.IsNullOrWhiteSpace(legalName))
            throw new ArgumentException("Legal name is required.", nameof(legalName));

        if (legalName.Length > 250)
            throw new ArgumentException("Legal name cannot exceed 250 characters.", nameof(legalName));

        LegalName = legalName.Trim();
        MarkUpdated();
    }

    public void UpdateDetails(
        string? tradeName,
        string? registrationNumber,
        string? taxNumber,
        string? website,
        string? primaryContactEmail,
        string? primaryContactPhone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? countryCode,
        string? currencyCode,
        string? timeZoneId)
    {
        TradeName = tradeName?.Trim();
        RegistrationNumber = registrationNumber?.Trim();
        TaxNumber = taxNumber?.Trim();
        Website = website?.Trim();
        PrimaryContactEmail = primaryContactEmail?.Trim();
        PrimaryContactPhone = primaryContactPhone?.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        StateOrProvince = stateOrProvince?.Trim();
        PostalCode = postalCode?.Trim();
        CountryCode = countryCode?.Trim()?.ToUpperInvariant();
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();

        MarkUpdated();
    }
}
