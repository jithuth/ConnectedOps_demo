using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Platform.Tenants;

public sealed class IndexModel : PageModel
{
    private readonly IPlatformTenantService _tenantService;

    public IndexModel(IPlatformTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public PlatformTenantPage TenantsPage { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public TenantStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Country { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool SortDescending { get; set; }

    public string? FeedbackMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var query = new PlatformTenantQuery(
            Search: Search,
            Status: Status,
            Country: Country,
            Page: PageIndex,
            PageSize: 10,
            SortBy: SortBy,
            SortDescending: SortDescending);

        TenantsPage = await _tenantService.GetTenantsAsync(query, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid tenantId)
    {
        try
        {
            await _tenantService.ActivateTenantAsync(tenantId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Tenant activated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to activate tenant: {ex.Message}";
        }

        return RedirectToPage(new { Search, Status, Country, PageIndex, SortBy, SortDescending });
    }

    public async Task<IActionResult> OnPostSuspendAsync(Guid tenantId)
    {
        try
        {
            await _tenantService.SuspendTenantAsync(tenantId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Tenant suspended successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to suspend tenant: {ex.Message}";
        }

        return RedirectToPage(new { Search, Status, Country, PageIndex, SortBy, SortDescending });
    }

    public async Task<IActionResult> OnPostDisableAsync(Guid tenantId)
    {
        try
        {
            await _tenantService.DisableTenantAsync(tenantId, HttpContext.RequestAborted);
            TempData["Feedback"] = "Tenant disabled successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to disable tenant: {ex.Message}";
        }

        return RedirectToPage(new { Search, Status, Country, PageIndex, SortBy, SortDescending });
    }

    public async Task<IActionResult> OnPostCreateAsync(CreatePlatformTenantRequest request)
    {
        try
        {
            var created = await _tenantService.CreateTenantAsync(request, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Tenant '{created.Name}' created successfully.";
            return RedirectToPage("/Platform/Tenants/Detail", new { id = created.Id });
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Failed to create tenant: {ex.Message}";
            return RedirectToPage();
        }
    }
}
