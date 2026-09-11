using ConnectedOps.Application.FleetOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class DashboardModel : PageModel
{
    private readonly IFleetOperationsDashboardService _dashboardService;

    public DashboardModel(IFleetOperationsDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public FleetOperationsDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardService.GetDashboardAsync(HttpContext.RequestAborted);
    }
}
