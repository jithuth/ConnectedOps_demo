using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Organization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization.Locations;

public sealed class IndexModel : PageModel
{
    private readonly ILocationService _locationService;
    private readonly IBranchService _branchService;

    public IndexModel(
        ILocationService locationService,
        IBranchService branchService)
    {
        _locationService = locationService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<LocationListItemDto> Locations { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty]
    public CreateLocationRequest CreateInput { get; set; } = null!;

    [BindProperty]
    public UpdateLocationRequest EditInput { get; set; } = null!;

    public async Task OnGetAsync()
    {
        Branches = await _branchService.GetBranchesAsync(HttpContext.RequestAborted);
        Locations = await _locationService.GetLocationsAsync(BranchId, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var loc = await _locationService.CreateLocationAsync(CreateInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Location '{loc.Name}' ({loc.Code}) was created successfully.";
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id)
    {
        try
        {
            var loc = await _locationService.UpdateLocationAsync(id, EditInput, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Location '{loc.Name}' ({loc.Code}) was updated successfully.";
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _locationService.DeleteLocationAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Location was deleted successfully.";
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage("/Organization/Locations/Index", new { BranchId });
        }
    }
}
