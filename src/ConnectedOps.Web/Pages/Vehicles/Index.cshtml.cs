using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class IndexModel : PageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IVehicleCategoryService _categoryService;
    private readonly IVehicleMakeModelService _makeModelService;
    private readonly IBranchService _branchService;

    public IndexModel(
        IVehicleService vehicleService,
        IVehicleCategoryService categoryService,
        IVehicleMakeModelService makeModelService,
        IBranchService branchService)
    {
        _vehicleService = vehicleService;
        _categoryService = categoryService;
        _makeModelService = makeModelService;
        _branchService = branchService;
    }

    public PagedResult<VehicleListItemDto> Vehicles { get; private set; } = null!;
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];
    public IReadOnlyCollection<VehicleMakeDto> Makes { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public VehicleQueryParameters Query { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Vehicles = await _vehicleService.GetVehiclesPagedAsync(Query, ct);
        Categories = await _categoryService.GetCategoriesAsync(false, ct);
        Makes = await _makeModelService.GetMakesAsync(false, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
    }

    public async Task<IActionResult> OnPostChangeStatusAsync(Guid id, VehicleStatus newStatus, string? notes)
    {
        try
        {
            await _vehicleService.ChangeStatusAsync(id, new ChangeVehicleStatusRequest(newStatus, notes), HttpContext.RequestAborted);
            TempData["Feedback"] = "Vehicle status was successfully updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            Query.SearchTerm,
            Query.CategoryId,
            Query.MakeId,
            Query.BranchId,
            Query.Status,
            Query.FuelType,
            Query.PageNumber,
            Query.PageSize
        });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _vehicleService.DeleteVehicleAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Vehicle was successfully removed.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new
        {
            Query.SearchTerm,
            Query.CategoryId,
            Query.MakeId,
            Query.BranchId,
            Query.Status,
            Query.FuelType,
            Query.PageNumber,
            Query.PageSize
        });
    }
}
