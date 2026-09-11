using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Plans;

[Authorize]
public class CreateModel : PageModel
{
    private readonly IMaintenancePlanService _planService;
    private readonly IVehicleCategoryService _categoryService;

    public CreateModel(
        IMaintenancePlanService planService,
        IVehicleCategoryService categoryService)
    {
        _planService = planService;
        _categoryService = categoryService;
    }

    [BindProperty]
    public CreateMaintenancePlanRequest Input { get; set; } = new(string.Empty, string.Empty);

    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetCategoriesAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            Categories = await _categoryService.GetCategoriesAsync();
            return Page();
        }

        try
        {
            var plan = await _planService.CreateAsync(Input);
            return RedirectToPage("/Maintenance/Plans/Details", new { id = plan.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Categories = await _categoryService.GetCategoriesAsync();
            return Page();
        }
    }
}
