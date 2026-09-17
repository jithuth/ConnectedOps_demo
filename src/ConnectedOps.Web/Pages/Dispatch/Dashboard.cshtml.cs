using ConnectedOps.Application.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Dispatch;

[Authorize]
public class DashboardModel : PageModel
{
    private readonly IDispatchService _dispatchService;

    public DashboardModel(IDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    public DispatchDashboardDto Metrics { get; private set; } = null!;

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Metrics = await _dispatchService.GetDashboardAsync(cancellationToken);
    }
}
