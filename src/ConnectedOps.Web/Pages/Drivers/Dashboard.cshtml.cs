using ConnectedOps.Application.Drivers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Drivers;

public sealed class DashboardModel : PageModel
{
    private readonly IDriverDashboardService _dashboardService;

    public DashboardModel(IDriverDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public DriverDashboardSummaryDto Summary { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Summary = await _dashboardService.GetDashboardSummaryAsync(HttpContext.RequestAborted);
    }
}
