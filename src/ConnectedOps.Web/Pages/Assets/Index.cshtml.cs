using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Assets;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetService _assetService;
    private readonly IAssetCategoryService _categoryService;
    private readonly IAssetTypeService _typeService;

    public IndexModel(
        IAssetService assetService,
        IAssetCategoryService categoryService,
        IAssetTypeService typeService)
    {
        _assetService = assetService;
        _categoryService = categoryService;
        _typeService = typeService;
    }

    public PagedResult<AssetDto> Assets { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? AssetTypeId { get; set; }

    [BindProperty(SupportsGet = true)]
    public AssetStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public AssetCondition? Condition { get; set; }

    [BindProperty(SupportsGet = true)]
    public AssetAssignmentType? AssignmentType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 15;

    public List<SelectListItem> Categories { get; private set; } = [];
    public List<SelectListItem> AssetTypes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var filter = new AssetListFilter(
            SearchTerm,
            CategoryId,
            AssetTypeId,
            Status,
            Condition,
            AssignmentType,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            PageNumber,
            PageSize);

        Assets = await _assetService.GetPagedAsync(filter, cancellationToken);

        var cats = await _categoryService.GetAllAsync(cancellationToken);
        Categories = cats.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == CategoryId)).ToList();

        var types = await _typeService.GetAllAsync(CategoryId, cancellationToken);
        AssetTypes = types.Select(t => new SelectListItem(t.Name, t.Id.ToString(), t.Id == AssetTypeId)).ToList();
    }
}
