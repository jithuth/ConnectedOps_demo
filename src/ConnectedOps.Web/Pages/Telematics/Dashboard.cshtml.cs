using ConnectedOps.Application.Telematics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Telematics;

[Authorize]
public sealed class DashboardModel : PageModel
{
    private readonly ITelematicsDashboardService _dashboardService;

    public DashboardModel(ITelematicsDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public TelematicsDashboardDto Dashboard { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Dashboard = await _dashboardService.GetDashboardAsync(HttpContext.RequestAborted);
    }
}
