using ConnectedOps.Application.Assets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Assets;

[Authorize]
public sealed class DashboardModel : PageModel
{
    private readonly IAssetDashboardService _dashboardService;
    private readonly IAssetUtilizationService _utilizationService;

    public DashboardModel(
        IAssetDashboardService dashboardService,
        IAssetUtilizationService utilizationService)
    {
        _dashboardService = dashboardService;
        _utilizationService = utilizationService;
    }

    public AssetDashboardSummaryDto Summary { get; private set; } = null!;
    public AssetUtilizationSummaryDto Utilization { get; private set; } = null!;
    public IReadOnlyList<AssetIdleReportDto> IdleAssets { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Summary = await _dashboardService.GetDashboardSummaryAsync(BranchId, cancellationToken);
        Utilization = await _utilizationService.GetUtilizationSummaryAsync(BranchId, null, cancellationToken);
        IdleAssets = await _utilizationService.GetIdleAssetsAsync(30, BranchId, cancellationToken);
    }
}
