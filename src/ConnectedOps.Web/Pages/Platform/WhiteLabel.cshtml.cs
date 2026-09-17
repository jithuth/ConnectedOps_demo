using ConnectedOps.Application.WhiteLabel;
using ConnectedOps.Domain.WhiteLabel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

[Authorize]
public class WhiteLabelModel : PageModel
{
    private readonly IWhiteLabelService _whiteLabelService;

    public WhiteLabelModel(IWhiteLabelService whiteLabelService)
    {
        _whiteLabelService = whiteLabelService;
    }

    public WhiteLabelOverviewDto Overview { get; private set; } = null!;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Overview = await _whiteLabelService.GetOverviewAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostUpdateBrandingAsync(
        string platformTitle,
        string? logoUrl,
        string? faviconUrl,
        string primaryAccentColor,
        string secondaryAccentColor,
        string? supportEmail,
        string? customLoginBannerUrl,
        string? customFooterText,
        bool isCustomBrandingEnabled,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new UpdateBrandingRequest(
                PlatformTitle: platformTitle,
                LogoUrl: logoUrl,
                FaviconUrl: faviconUrl,
                PrimaryAccentColor: primaryAccentColor,
                SecondaryAccentColor: secondaryAccentColor,
                SupportEmail: supportEmail,
                CustomLoginBannerUrl: customLoginBannerUrl,
                CustomFooterText: customFooterText,
                IsCustomBrandingEnabled: isCustomBrandingEnabled);

            await _whiteLabelService.UpdateBrandingAsync(req, cancellationToken);
            SuccessMessage = "Tenant white-label branding identity updated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRegisterDomainAsync(
        string hostname,
        CancellationToken cancellationToken)
    {
        try
        {
            var req = new RegisterCustomDomainRequest(hostname);
            await _whiteLabelService.RegisterCustomDomainAsync(req, cancellationToken);
            SuccessMessage = $"Custom vanity domain '{hostname}' registered. Configure DNS CNAME records to verify.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostVerifyDomainAsync(
        Guid domainId,
        CancellationToken cancellationToken)
    {
        try
        {
            var domain = await _whiteLabelService.VerifyCustomDomainAsync(new VerifyCustomDomainRequest(domainId), cancellationToken);
            if (domain.Status == DomainVerificationStatus.Verified)
            {
                SuccessMessage = $"Custom domain '{domain.Hostname}' verified! SSL certificate automatically provisioned.";
            }
            else
            {
                ErrorMessage = $"DNS verification failed for '{domain.Hostname}'. Ensure CNAME points to ingress.connectedops.io.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostGenerateAuditPackageAsync(
        AuditPackageType packageType,
        CancellationToken cancellationToken)
    {
        try
        {
            var package = await _whiteLabelService.GenerateAuditPackageAsync(new GenerateAuditPackageRequest(packageType), cancellationToken);
            SuccessMessage = $"Audit compliance evidence package '{package.PackageNumber}' generated with SHA-256 checksum {package.ChecksumSha256[..12]}...";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
