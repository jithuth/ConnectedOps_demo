using ConnectedOps.Application.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Safety;

[Authorize]
public sealed class DashboardModel : PageModel
{
    private readonly ISafetyDashboardService _dashboardService;

    public DashboardModel(ISafetyDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public SafetyDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
    }
}
