using ConnectedOps.Domain.WhiteLabel;

namespace ConnectedOps.Application.WhiteLabel;

public sealed record TenantBrandingDto(
    Guid TenantId,
    string PlatformTitle,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryAccentColor,
    string SecondaryAccentColor,
    string? SupportEmail,
    string? CustomLoginBannerUrl,
    string? CustomFooterText,
    bool IsCustomBrandingEnabled,
    DateTime UpdatedAtUtc);

public sealed record UpdateBrandingRequest(
    string PlatformTitle,
    string? LogoUrl,
    string? FaviconUrl,
    string PrimaryAccentColor,
    string SecondaryAccentColor,
    string? SupportEmail,
    string? CustomLoginBannerUrl,
    string? CustomFooterText,
    bool IsCustomBrandingEnabled);

public sealed record TenantCustomDomainDto(
    Guid Id,
    Guid TenantId,
    string Hostname,
    string VerificationToken,
    DomainVerificationStatus Status,
    bool SslProvisioned,
    DateTime CreatedAtUtc,
    DateTime? VerifiedAtUtc);

public sealed record RegisterCustomDomainRequest(
    string Hostname);

public sealed record VerifyCustomDomainRequest(
    Guid DomainId);

public sealed record AuditCompliancePackageDto(
    Guid Id,
    Guid TenantId,
    string PackageNumber,
    AuditPackageType PackageType,
    DateTime GeneratedAtUtc,
    Guid GeneratedByUserId,
    int EvidenceItemsCount,
    string ChecksumSha256,
    long FileSizeBytes,
    string ManifestJson);

public sealed record GenerateAuditPackageRequest(
    AuditPackageType PackageType);

public sealed record WhiteLabelOverviewDto(
    TenantBrandingDto Branding,
    IReadOnlyList<TenantCustomDomainDto> CustomDomains,
    IReadOnlyList<AuditCompliancePackageDto> RecentAuditPackages);
