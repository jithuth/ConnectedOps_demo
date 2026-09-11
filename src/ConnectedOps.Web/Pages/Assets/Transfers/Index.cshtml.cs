using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets.Transfers;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetTransferService _transferService;

    public IndexModel(IAssetTransferService transferService)
    {
        _transferService = transferService;
    }

    public PagedResult<AssetTransferDto> Transfers { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public AssetTransferStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public AssetTransferType? TransferType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 20;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var filter = new AssetTransferFilter(
            null,
            TransferType,
            Status,
            null,
            null,
            null,
            PageNumber,
            PageSize);

        Transfers = await _transferService.GetPagedAsync(filter, cancellationToken);
    }
}
