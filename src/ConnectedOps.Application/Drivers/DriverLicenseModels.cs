namespace ConnectedOps.Application.Drivers;

public sealed record DriverLicenseCategoryDto(
    Guid Id,
    Guid DriverLicenseId,
    string CategoryCode,
    string? Description,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool IsActive);

public sealed record DriverLicenseDto(
    Guid Id,
    Guid DriverId,
    string LicenseNumber,
    string LicenseCountryCode,
    string? IssuingAuthority,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsPrimary,
    bool IsActive,
    bool IsExpired,
    bool IsExpiringSoon,
    string? Notes,
    IReadOnlyCollection<DriverLicenseCategoryDto> Categories);

public sealed record CreateDriverLicenseRequest(
    string LicenseNumber,
    string LicenseCountryCode,
    string? IssuingAuthority = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    bool IsPrimary = false,
    string? Notes = null,
    IReadOnlyCollection<string>? CategoryCodes = null);

public sealed record UpdateDriverLicenseRequest(
    string LicenseNumber,
    string LicenseCountryCode,
    string? IssuingAuthority = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    string? Notes = null);

public sealed record AddDriverLicenseCategoryRequest(
    string CategoryCode,
    string? Description = null,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record UpdateDriverLicenseCategoryRequest(
    string? Description = null,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);
