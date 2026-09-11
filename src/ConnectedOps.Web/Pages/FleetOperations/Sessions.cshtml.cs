using ConnectedOps.Application.Drivers;
using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.FleetOperations;

[Authorize]
public sealed class SessionsModel : PageModel
{
    private readonly IVehicleUsageSessionService _sessionService;
    private readonly IVehicleService _vehicleService;
    private readonly IDriverService _driverService;
    private readonly IBranchService _branchService;

    public SessionsModel(
        IVehicleUsageSessionService sessionService,
        IVehicleService vehicleService,
        IDriverService driverService,
        IBranchService branchService)
    {
        _sessionService = sessionService;
        _vehicleService = vehicleService;
        _driverService = driverService;
        _branchService = branchService;
    }

    public PagedResult<VehicleUsageSessionDto> Sessions { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];
    public IReadOnlyCollection<DriverListItemDto> Drivers { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public UsageSessionQueryParameters Query { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Sessions = await _sessionService.GetSessionsPagedAsync(Query, ct);

        var vPaged = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 100 }, ct);
        Vehicles = vPaged.Items;

        var dPaged = await _driverService.GetDriversPagedAsync(new DriverQueryParameters { PageSize = 100 }, ct);
        Drivers = dPaged.Items;

        Branches = await _branchService.GetBranchesAsync(ct);
    }

    public async Task<IActionResult> OnPostCancelSessionAsync(Guid sessionId, string? reason)
    {
        try
        {
            await _sessionService.CancelSessionAsync(sessionId, reason, HttpContext.RequestAborted);
            StatusMessage = "Usage session was cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { Query.VehicleId, Query.DriverId, Query.Status, Query.PageNumber });
    }
}
