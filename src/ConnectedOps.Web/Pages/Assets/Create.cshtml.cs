using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Assets;

[Authorize]
public sealed class CreateModel : PageModel
{
    private readonly IAssetService _assetService;
    private readonly IAssetCategoryService _categoryService;
    private readonly IAssetTypeService _typeService;

    public CreateModel(
        IAssetService assetService,
        IAssetCategoryService categoryService,
        IAssetTypeService typeService)
    {
        _assetService = assetService;
        _categoryService = categoryService;
        _typeService = typeService;
    }

    [BindProperty]
    public CreateAssetRequest Form { get; set; } = new(
        AssetNumber: string.Empty,
        Name: string.Empty,
        Description: null,
        SerialNumber: null,
        InternalCode: null,
        Barcode: null,
        RfidTag: null,
        CategoryId: Guid.Empty,
        AssetTypeId: Guid.Empty,
        Make: null,
        Model: null,
        Year: null,
        OwnershipType: AssetOwnershipType.CompanyOwned,
        Condition: AssetCondition.Good);

    public List<SelectListItem> Categories { get; private set; } = [];
    public List<SelectListItem> AssetTypes { get; private set; } = [];
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadDropdownsAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Form.AssetNumber) || string.IsNullOrWhiteSpace(Form.Name) || Form.CategoryId == Guid.Empty || Form.AssetTypeId == Guid.Empty)
        {
            ErrorMessage = "Please complete all required fields (Asset Number, Name, Category, and Asset Type).";
            await LoadDropdownsAsync(cancellationToken);
            return Page();
        }

        try
        {
            var created = await _assetService.CreateAsync(Form, cancellationToken);
            return RedirectToPage("/Assets/Details", new { id = created.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDropdownsAsync(cancellationToken);
            return Page();
        }
    }

    private async Task LoadDropdownsAsync(CancellationToken cancellationToken)
    {
        var cats = await _categoryService.GetAllAsync(cancellationToken);
        Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == Form.CategoryId)).ToList();

        var types = await _typeService.GetAllAsync(Form.CategoryId != Guid.Empty ? Form.CategoryId : null, cancellationToken);
        AssetTypes = types.Select(t => new SelectListItem(t.Name, t.Id.ToString(), t.Id == Form.AssetTypeId)).ToList();
    }
}
