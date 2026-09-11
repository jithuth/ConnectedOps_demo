using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets.Custody;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetCustodyService _custodyService;

    public IndexModel(IAssetCustodyService custodyService)
    {
        _custodyService = custodyService;
    }

    public PagedResult<AssetUsageSessionDto> Sessions { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public bool? ActiveOnly { get; set; } = true;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Sessions = await _custodyService.GetUsageSessionsPagedAsync(
            null,
            null,
            null,
            ActiveOnly,
            PageNumber,
            PageSize,
            cancellationToken);
    }
}
