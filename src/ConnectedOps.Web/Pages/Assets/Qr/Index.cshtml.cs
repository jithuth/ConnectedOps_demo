using ConnectedOps.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets.Qr;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly IAssetService _assetService;
    private readonly IAssetIdentifierService _identifierService;

    public IndexModel(
        IAssetService assetService,
        IAssetIdentifierService identifierService)
    {
        _assetService = assetService;
        _identifierService = identifierService;
    }

    public IReadOnlyList<AssetDto> Assets { get; private set; } = [];
    public AssetScanResultDto? ScanResult { get; private set; }
    public string? ScannedToken { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(string? token, CancellationToken cancellationToken)
    {
        var result = await _assetService.GetPagedAsync(new AssetListFilter(PageSize: 50), cancellationToken);
        Assets = result.Items.ToList();

        if (!string.IsNullOrWhiteSpace(token))
        {
            ScannedToken = token;
            ScanResult = await _identifierService.ScanByTokenAsync(token, cancellationToken);
            if (ScanResult is null)
            {
                ErrorMessage = "Tag token could not be verified or is not registered.";
            }
        }
    }
}
