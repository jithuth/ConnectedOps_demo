using ConnectedOps.Application.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maintenance;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IMaintenanceDashboardService _dashboardService;

    public DashboardModel(IMaintenanceDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public MaintenanceDashboardDto Metrics { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Metrics = await _dashboardService.GetDashboardMetricsAsync();
    }
}
