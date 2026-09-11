using ConnectedOps.Application.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IFuelDashboardService _dashboardService;

    public DashboardModel(IFuelDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public FuelDashboardDto Metrics { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Metrics = await _dashboardService.GetDashboardMetricsAsync();
    }
}
