using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets.Inspections;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetInspectionService _inspectionService;

    public IndexModel(IAssetInspectionService inspectionService)
    {
        _inspectionService = inspectionService;
    }

    public PagedResult<AssetInspectionDto> Inspections { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public AssetInspectionType? InspectionType { get; set; }

    [BindProperty(SupportsGet = true)]
    public AssetInspectionResult? Result { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var filter = new AssetInspectionFilter(
            null,
            InspectionType,
            null,
            Result,
            null,
            null,
            PageNumber,
            PageSize);

        Inspections = await _inspectionService.GetInspectionsPagedAsync(filter, cancellationToken);
    }
}
