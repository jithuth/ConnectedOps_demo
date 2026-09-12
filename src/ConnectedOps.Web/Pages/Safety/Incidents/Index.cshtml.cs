using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Safety;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Safety;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ConnectedOps.Web.Pages.Safety.Incidents;

[Authorize]
public sealed class IndexModel : PageModel
{
    private readonly ISafetyIncidentService _incidentService;
    private readonly IBranchService _branchService;

    public IndexModel(
        ISafetyIncidentService incidentService,
        IBranchService branchService)
    {
        _incidentService = incidentService;
        _branchService = branchService;
    }

    [BindProperty(SupportsGet = true)]
    public SafetyIncidentStatus? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public SafetyIncidentSeverity? Severity { get; set; }

    [BindProperty(SupportsGet = true)]
    public SafetyIncidentType? IncidentType { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public PagedResult<SafetyIncidentDto> Incidents { get; private set; } = null!;
    public List<SelectListItem> Branches { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var branches = await _branchService.GetBranchesAsync(cancellationToken);
        Branches = branches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == BranchId)).ToList();

        var filter = new SafetyIncidentFilterRequest(
            Status: Status,
            Severity: Severity,
            IncidentType: IncidentType,
            BranchId: BranchId,
            SearchTerm: SearchTerm,
            Page: PageNumber,
            PageSize: 20);

        Incidents = await _incidentService.GetPagedAsync(filter, cancellationToken);
    }
}
