using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization;

public sealed class SettingsModel : PageModel
{
    private readonly IOrganizationSettingsService _settingsService;

    public SettingsModel(IOrganizationSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [BindProperty]
    public UpdateOrganizationSettingsRequest Input { get; set; } = null!;

    public OrganizationSettingsDto Settings { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Settings = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
        Input = new UpdateOrganizationSettingsRequest(
            Settings.EnforceBranchAssignment,
            Settings.EnforceDepartmentAssignment,
            Settings.AutoCreateEmployeeForUser,
            Settings.FiscalYearStartMonth,
            Settings.DefaultWorkingDaysJson);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var updated = await _settingsService.UpdateSettingsAsync(Input, HttpContext.RequestAborted);
            TempData["Feedback"] = "Organization governance settings updated successfully.";
            return RedirectToPage("/Organization/Settings");
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            Settings = await _settingsService.GetSettingsAsync(HttpContext.RequestAborted);
            return Page();
        }
    }
}
