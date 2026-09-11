using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Organization;

public sealed class DashboardModel : PageModel
{
    private readonly IOrganizationDashboardService _dashboardService;

    public DashboardModel(IOrganizationDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public OrganizationDashboardSummaryDto Summary { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Summary = await _dashboardService.GetDashboardSummaryAsync(HttpContext.RequestAborted);
    }
}
