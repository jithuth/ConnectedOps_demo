using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Vehicles;

public sealed class DashboardModel : PageModel
{
    private readonly IVehicleDashboardService _dashboardService;

    public DashboardModel(IVehicleDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public VehicleDashboardStatsDto Stats { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Stats = await _dashboardService.GetDashboardStatsAsync(HttpContext.RequestAborted);
    }
}
