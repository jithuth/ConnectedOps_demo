using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class MakesModelsModel : PageModel
{
    private readonly IVehicleMakeModelService _makeModelService;
    private readonly IVehicleCategoryService _categoryService;

    public MakesModelsModel(
        IVehicleMakeModelService makeModelService,
        IVehicleCategoryService categoryService)
    {
        _makeModelService = makeModelService;
        _categoryService = categoryService;
    }

    public IReadOnlyCollection<VehicleMakeDto> Makes { get; private set; } = [];
    public IReadOnlyCollection<VehicleModelDto> Models { get; private set; } = [];
    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public string ActiveTab { get; set; } = "makes";

    [BindProperty(SupportsGet = true)]
    public Guid? SelectedMakeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; } = false;

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Makes = await _makeModelService.GetMakesAsync(IncludeInactive, ct);
        Categories = await _categoryService.GetCategoriesAsync(false, ct);

        if (SelectedMakeId.HasValue && SelectedMakeId.Value != Guid.Empty)
        {
            Models = await _makeModelService.GetModelsByMakeIdAsync(SelectedMakeId.Value, IncludeInactive, ct);
        }
        else
        {
            Models = await _makeModelService.GetAllModelsAsync(IncludeInactive, ct);
        }
    }

    public async Task<IActionResult> OnPostCreateMakeAsync(string name, string? countryCode)
    {
        try
        {
            var req = new CreateVehicleMakeRequest(name, countryCode);
            await _makeModelService.CreateMakeAsync(req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Make '{name}' created successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "makes", IncludeInactive });
    }

    public async Task<IActionResult> OnPostEditMakeAsync(Guid id, string name, string? countryCode)
    {
        try
        {
            var req = new UpdateVehicleMakeRequest(name, countryCode);
            await _makeModelService.UpdateMakeAsync(id, req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Make '{name}' updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "makes", IncludeInactive });
    }

    public async Task<IActionResult> OnPostDeleteMakeAsync(Guid id)
    {
        try
        {
            await _makeModelService.DeleteMakeAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Make deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "makes", IncludeInactive });
    }

    public async Task<IActionResult> OnPostCreateModelAsync(Guid vehicleMakeId, string name, Guid? defaultCategoryId)
    {
        try
        {
            var req = new CreateVehicleModelRequest(vehicleMakeId, name, defaultCategoryId);
            await _makeModelService.CreateModelAsync(req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Model '{name}' created successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "models", SelectedMakeId = vehicleMakeId, IncludeInactive });
    }

    public async Task<IActionResult> OnPostEditModelAsync(Guid id, string name, Guid? defaultCategoryId, Guid? returnMakeId)
    {
        try
        {
            var req = new UpdateVehicleModelRequest(name, defaultCategoryId);
            await _makeModelService.UpdateModelAsync(id, req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Model '{name}' updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "models", SelectedMakeId = returnMakeId, IncludeInactive });
    }

    public async Task<IActionResult> OnPostDeleteModelAsync(Guid id, Guid? returnMakeId)
    {
        try
        {
            await _makeModelService.DeleteModelAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Model deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { ActiveTab = "models", SelectedMakeId = returnMakeId, IncludeInactive });
    }
}
