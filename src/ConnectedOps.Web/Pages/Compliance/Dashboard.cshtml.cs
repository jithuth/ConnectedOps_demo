using ConnectedOps.Application.Compliance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Compliance;

[Authorize]
public sealed class DashboardModel : PageModel
{
    private readonly IComplianceDashboardService _dashboardService;

    public DashboardModel(IComplianceDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public ComplianceDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
    }
}
