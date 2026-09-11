using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization;

public sealed class ProfileModel : PageModel
{
    private readonly IOrganizationProfileService _profileService;

    public ProfileModel(IOrganizationProfileService profileService)
    {
        _profileService = profileService;
    }

    [BindProperty]
    public UpdateOrganizationProfileRequest Input { get; set; } = null!;

    public OrganizationProfileDto Profile { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Profile = await _profileService.GetProfileAsync(HttpContext.RequestAborted);
        Input = new UpdateOrganizationProfileRequest(
            Profile.LegalName,
            Profile.TradeName,
            Profile.RegistrationNumber,
            Profile.TaxNumber,
            Profile.Website,
            Profile.PrimaryContactEmail,
            Profile.PrimaryContactPhone,
            Profile.AddressLine1,
            Profile.AddressLine2,
            Profile.City,
            Profile.StateOrProvince,
            Profile.PostalCode,
            Profile.CountryCode,
            Profile.CurrencyCode,
            Profile.TimeZoneId);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var updated = await _profileService.UpdateProfileAsync(Input, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Organization profile for '{updated.LegalName}' updated successfully.";
            return RedirectToPage("/Organization/Profile");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            Profile = await _profileService.GetProfileAsync(HttpContext.RequestAborted);
            return Page();
        }
    }
}
