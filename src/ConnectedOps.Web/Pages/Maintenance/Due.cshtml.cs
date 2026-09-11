using ConnectedOps.Application.Maintenance;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance;

[Authorize]
public class DueModel : PageModel
{
    private readonly IMaintenanceScheduleService _scheduleService;
    private readonly IMaintenanceServiceTypeService _serviceTypeService;
    private readonly IBranchService _branchService;
    private readonly IVehicleCategoryService _categoryService;

    public DueModel(
        IMaintenanceScheduleService scheduleService,
        IMaintenanceServiceTypeService serviceTypeService,
        IBranchService branchService,
        IVehicleCategoryService categoryService)
    {
        _scheduleService = scheduleService;
        _serviceTypeService = serviceTypeService;
        _branchService = branchService;
        _categoryService = categoryService;
    }

    [BindProperty(SupportsGet = true)]
    public MaintenanceDueQueryParameters Query { get; set; } = new();

    public PagedResult<VehicleMaintenanceDueDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<MaintenanceServiceTypeDto> ServiceTypes { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    public int UpcomingCount { get; private set; }
    public int DueCount { get; private set; }
    public int OverdueCount { get; private set; }

    public async Task OnGetAsync()
    {
        var allItems = await _scheduleService.CalculateAllVehiclesMaintenanceAsync();
        UpcomingCount = allItems.Count(x => x.DueStatus == MaintenanceDueStatus.Upcoming);
        DueCount = allItems.Count(x => x.DueStatus == MaintenanceDueStatus.Due);
        OverdueCount = allItems.Count(x => x.DueStatus == MaintenanceDueStatus.Overdue);

        Result = await _scheduleService.GetDueMaintenanceAsync(Query);
        ServiceTypes = await _serviceTypeService.GetAllAsync(activeOnly: true);
        Branches = await _branchService.GetBranchesAsync();
        Categories = await _categoryService.GetCategoriesAsync();
    }
}
