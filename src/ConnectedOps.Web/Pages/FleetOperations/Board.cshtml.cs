using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.FleetOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class BoardModel : PageModel
{
    private readonly IFleetOperationsDashboardService _dashboardService;
    private readonly IBranchService _branchService;

    public BoardModel(
        IFleetOperationsDashboardService dashboardService,
        IBranchService branchService)
    {
        _dashboardService = dashboardService;
        _branchService = branchService;
    }

    public FleetOperationsBoardDto Board { get; private set; } = null!;
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public FleetAvailabilityFilter Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Board = await _dashboardService.GetBoardAsync(Filter, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
    }
}
