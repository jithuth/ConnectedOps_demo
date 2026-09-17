using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.WhiteLabel;

public sealed class TenantBranding : BaseEntity
{
    private TenantBranding()
    {
    }

    public TenantBranding(
        Guid tenantId,
        string platformTitle = "ConnectedOps",
        string? logoUrl = null,
        string? faviconUrl = null,
        string primaryAccentColor = "#0d6efd",
        string secondaryAccentColor = "#6c757d",
        string? supportEmail = null,
        string? customLoginBannerUrl = null,
        string? customFooterText = null,
        bool isCustomBrandingEnabled = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        PlatformTitle = string.IsNullOrWhiteSpace(platformTitle) ? "ConnectedOps" : platformTitle.Trim();
        LogoUrl = logoUrl?.Trim();
        FaviconUrl = faviconUrl?.Trim();
        PrimaryAccentColor = string.IsNullOrWhiteSpace(primaryAccentColor) ? "#0d6efd" : primaryAccentColor.Trim();
        SecondaryAccentColor = string.IsNullOrWhiteSpace(secondaryAccentColor) ? "#6c757d" : secondaryAccentColor.Trim();
        SupportEmail = supportEmail?.Trim();
        CustomLoginBannerUrl = customLoginBannerUrl?.Trim();
        CustomFooterText = customFooterText?.Trim();
        IsCustomBrandingEnabled = isCustomBrandingEnabled;
    }

    public Guid TenantId { get; private set; }
    public string PlatformTitle { get; private set; } = "ConnectedOps";
    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string PrimaryAccentColor { get; private set; } = "#0d6efd";
    public string SecondaryAccentColor { get; private set; } = "#6c757d";
    public string? SupportEmail { get; private set; }
    public string? CustomLoginBannerUrl { get; private set; }
    public string? CustomFooterText { get; private set; }
    public bool IsCustomBrandingEnabled { get; private set; }

    public void UpdateBranding(
        string platformTitle,
        string? logoUrl,
        string? faviconUrl,
        string primaryAccentColor,
        string secondaryAccentColor,
        string? supportEmail,
        string? customLoginBannerUrl,
        string? customFooterText,
        bool isEnabled)
    {
        PlatformTitle = string.IsNullOrWhiteSpace(platformTitle) ? "ConnectedOps" : platformTitle.Trim();
        LogoUrl = logoUrl?.Trim();
        FaviconUrl = faviconUrl?.Trim();
        PrimaryAccentColor = string.IsNullOrWhiteSpace(primaryAccentColor) ? "#0d6efd" : primaryAccentColor.Trim();
        SecondaryAccentColor = string.IsNullOrWhiteSpace(secondaryAccentColor) ? "#6c757d" : secondaryAccentColor.Trim();
        SupportEmail = supportEmail?.Trim();
        CustomLoginBannerUrl = customLoginBannerUrl?.Trim();
        CustomFooterText = customFooterText?.Trim();
        IsCustomBrandingEnabled = isEnabled;
        MarkUpdated();
    }
}
