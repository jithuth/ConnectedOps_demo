using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance.Plans;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IMaintenancePlanService _planService;
    private readonly IVehicleCategoryService _categoryService;

    public IndexModel(
        IMaintenancePlanService planService,
        IVehicleCategoryService categoryService)
    {
        _planService = planService;
        _categoryService = categoryService;
    }

    public IReadOnlyCollection<MaintenancePlanListItemDto> Plans { get; private set; } = [];
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    public async Task OnGetAsync(Guid? vehicleCategoryId)
    {
        Plans = await _planService.GetAllAsync(vehicleCategoryId: vehicleCategoryId);
        Categories = await _categoryService.GetCategoriesAsync();
    }
}
