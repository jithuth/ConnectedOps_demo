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
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Summary = await _dashboardService.GetDashboardSummaryAsync(HttpContext.RequestAborted);
        }
        catch (UnauthorizedAccessException ex)
        {
            ErrorMessage = ex.Message;
            Summary = new OrganizationDashboardSummaryDto(
                0, 0, 0, 0, 0, 0, 0, 0,
                new Dictionary<string, int>(),
                new Dictionary<string, int>(),
                new Dictionary<string, int>());
        }
    }
}
