using ConnectedOps.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets.Categories;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetCategoryService _categoryService;
    private readonly IAssetTypeService _typeService;

    public IndexModel(
        IAssetCategoryService categoryService,
        IAssetTypeService typeService)
    {
        _categoryService = categoryService;
        _typeService = typeService;
    }

    public IReadOnlyList<AssetCategoryTreeDto> CategoryTree { get; private set; } = [];
    public IReadOnlyList<AssetTypeDto> AllTypes { get; private set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CategoryTree = await _categoryService.GetTreeAsync(cancellationToken);
        AllTypes = await _typeService.GetAllAsync(null, cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateCategoryAsync(
        string code,
        string name,
        string? description,
        Guid? parentCategoryId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _categoryService.CreateAsync(new CreateAssetCategoryRequest(code, name, description, parentCategoryId, true), cancellationToken);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync(cancellationToken);
            return Page();
        }
    }

    public async Task<IActionResult> OnPostCreateTypeAsync(
        Guid categoryId,
        string code,
        string name,
        string? description,
        int? defaultInspectionIntervalDays,
        int? defaultCalibrationIntervalDays,
        CancellationToken cancellationToken)
    {
        try
        {
            await _typeService.CreateAsync(new CreateAssetTypeRequest(
                categoryId,
                code,
                name,
                description,
                defaultInspectionIntervalDays.HasValue,
                defaultInspectionIntervalDays,
                defaultCalibrationIntervalDays.HasValue,
                defaultCalibrationIntervalDays,
                false,
                true), cancellationToken);
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await OnGetAsync(cancellationToken);
            return Page();
        }
    }
}
