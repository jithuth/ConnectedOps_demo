namespace ConnectedOps.Application.Organization;

public sealed record OrganizationProfileDto(
    Guid Id,
    Guid TenantId,
    string LegalName,
    string? TradeName,
    string? RegistrationNumber,
    string? TaxNumber,
    string? Website,
    string? PrimaryContactEmail,
    string? PrimaryContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string CurrencyCode,
    string TimeZoneId,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record UpdateOrganizationProfileRequest(
    string LegalName,
    string? TradeName,
    string? RegistrationNumber,
    string? TaxNumber,
    string? Website,
    string? PrimaryContactEmail,
    string? PrimaryContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string CurrencyCode,
    string TimeZoneId);
