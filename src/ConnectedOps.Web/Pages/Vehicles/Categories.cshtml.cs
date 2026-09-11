using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class CategoriesModel : PageModel
{
    private readonly IVehicleCategoryService _categoryService;

    public CategoriesModel(IVehicleCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public IReadOnlyCollection<VehicleCategoryDto> Categories { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public bool IncludeInactive { get; set; } = false;

    public async Task OnGetAsync()
    {
        Categories = await _categoryService.GetCategoriesAsync(IncludeInactive, HttpContext.RequestAborted);
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, string code, string? description, bool isMotorized)
    {
        try
        {
            var req = new CreateVehicleCategoryRequest(name, code, description, isMotorized);
            await _categoryService.CreateCategoryAsync(req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Category '{name}' created successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { IncludeInactive });
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, string name, string? description, bool isMotorized)
    {
        try
        {
            var req = new UpdateVehicleCategoryRequest(name, description, isMotorized);
            await _categoryService.UpdateCategoryAsync(id, req, HttpContext.RequestAborted);
            TempData["Feedback"] = $"Category '{name}' updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { IncludeInactive });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, bool activate)
    {
        try
        {
            if (activate)
            {
                await _categoryService.ActivateCategoryAsync(id, HttpContext.RequestAborted);
                TempData["Feedback"] = "Category activated successfully.";
            }
            else
            {
                await _categoryService.DeactivateCategoryAsync(id, HttpContext.RequestAborted);
                TempData["Feedback"] = "Category deactivated successfully.";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { IncludeInactive });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _categoryService.DeleteCategoryAsync(id, HttpContext.RequestAborted);
            TempData["Feedback"] = "Category deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage(new { IncludeInactive });
    }
}
