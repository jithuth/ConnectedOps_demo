using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Platform;

public sealed class PlatformSettings : BaseEntity
{
    private PlatformSettings()
    {
    }

    public PlatformSettings(
        string platformName,
        string companyName,
        string supportEmail,
        string defaultLanguage = "en",
        string defaultTimeZone = "UTC")
    {
        Update(platformName, companyName, supportEmail, defaultLanguage, defaultTimeZone);
    }

    public string PlatformName { get; private set; } = "ConnectedOps";

    public string CompanyName { get; private set; } = "ConnectedOps Technologies";

    public string SupportEmail { get; private set; } = "support@connectedops.com";

    public string DefaultLanguage { get; private set; } = "en";

    public string DefaultTimeZone { get; private set; } = "UTC";

    public void Update(
        string platformName,
        string companyName,
        string supportEmail,
        string defaultLanguage,
        string defaultTimeZone)
    {
        if (string.IsNullOrWhiteSpace(platformName))
            throw new ArgumentException("Platform name is required.", nameof(platformName));

        if (string.IsNullOrWhiteSpace(companyName))
            throw new ArgumentException("Company name is required.", nameof(companyName));

        if (string.IsNullOrWhiteSpace(supportEmail))
            throw new ArgumentException("Support email is required.", nameof(supportEmail));

        PlatformName = platformName.Trim();
        CompanyName = companyName.Trim();
        SupportEmail = supportEmail.Trim().ToLowerInvariant();
        DefaultLanguage = string.IsNullOrWhiteSpace(defaultLanguage) ? "en" : defaultLanguage.Trim().ToLowerInvariant();
        DefaultTimeZone = string.IsNullOrWhiteSpace(defaultTimeZone) ? "UTC" : defaultTimeZone.Trim();

        MarkUpdated();
    }
}
