using ConnectedOps.Application.Platform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform;

public sealed class SettingsModel : PageModel
{
    private readonly IPlatformSettingsService _settingsService;

    public SettingsModel(IPlatformSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public UpdatePlatformSettingsRequest SettingsForm { get; set; } = null!;

    public async Task OnGetAsync()
    {
        var settings = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
        SettingsForm = new UpdatePlatformSettingsRequest(
            settings.PlatformName,
            settings.CompanyName,
            settings.SupportEmail,
            settings.DefaultLanguage,
            settings.DefaultTimeZone);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var updated = await _settingsService.UpdateSettingsAsync(SettingsForm, HttpContext.RequestAborted);
            TempData["Feedback"] = "Platform settings updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to update settings: {ex.Message}";
        }

        return RedirectToPage();
    }
}
