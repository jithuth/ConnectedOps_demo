using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Assets;

[Authorize]
public sealed class EditModel : PageModel
{
    private readonly IAssetService _assetService;
    private readonly IAssetCategoryService _categoryService;
    private readonly IAssetTypeService _typeService;

    public EditModel(
        IAssetService assetService,
        IAssetCategoryService categoryService,
        IAssetTypeService typeService)
    {
        _assetService = assetService;
        _categoryService = categoryService;
        _typeService = typeService;
    }

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string AssetNumber { get; set; } = string.Empty;

    [BindProperty]
    public UpdateAssetRequest Form { get; set; } = null!;

    public List<SelectListItem> Categories { get; private set; } = [];
    public List<SelectListItem> AssetTypes { get; private set; } = [];
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        Id = id;
        var asset = await _assetService.GetByIdAsync(id, cancellationToken);
        if (asset is null) return NotFound();

        AssetNumber = asset.AssetNumber;
        Form = new UpdateAssetRequest(
            Name: asset.Name,
            Description: asset.Description,
            SerialNumber: asset.SerialNumber,
            InternalCode: asset.InternalCode,
            Barcode: asset.Barcode,
            RfidTag: asset.RfidTag,
            CategoryId: asset.CategoryId,
            AssetTypeId: asset.AssetTypeId,
            Make: asset.Make,
            Model: asset.Model,
            Year: asset.Year,
            OwnershipType: asset.OwnershipType,
            DepartmentId: asset.DepartmentId,
            TeamId: asset.TeamId,
            PurchaseCost: asset.PurchaseCost,
            PurchaseDate: asset.PurchaseDate,
            VendorName: asset.VendorName,
            WarrantyExpiryDate: asset.WarrantyExpiryDate,
            WarrantyNotes: asset.WarrantyNotes,
            ExpectedLifespanMonths: asset.ExpectedLifespanMonths,
            IsCritical: asset.IsCritical,
            IsActive: asset.IsActive);

        await LoadDropdownsAsync(asset.CategoryId, asset.AssetTypeId, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Form.Name))
        {
            ErrorMessage = "Asset Name is required.";
            await LoadDropdownsAsync(Form.CategoryId, Form.AssetTypeId, cancellationToken);
            return Page();
        }

        try
        {
            await _assetService.UpdateAsync(Id, Form, cancellationToken);
            return RedirectToPage("/Assets/Details", new { id = Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            await LoadDropdownsAsync(Form.CategoryId, Form.AssetTypeId, cancellationToken);
            return Page();
        }
    }

    private async Task LoadDropdownsAsync(Guid categoryId, Guid assetTypeId, CancellationToken cancellationToken)
    {
        var cats = await _categoryService.GetAllAsync(cancellationToken);
        Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == categoryId)).ToList();

        var types = await _typeService.GetAllAsync(categoryId != Guid.Empty ? categoryId : null, cancellationToken);
        AssetTypes = types.Select(t => new SelectListItem(t.Name, t.Id.ToString(), t.Id == assetTypeId)).ToList();
    }
}
