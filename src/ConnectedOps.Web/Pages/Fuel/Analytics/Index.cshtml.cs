using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Organization;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Analytics;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFuelAnalyticsService _analyticsService;
    private readonly IBranchService _branchService;
    private readonly IVehicleService _vehicleService;

    public IndexModel(
        IFuelAnalyticsService analyticsService,
        IBranchService branchService,
        IVehicleService vehicleService)
    {
        _analyticsService = analyticsService;
        _branchService = branchService;
        _vehicleService = vehicleService;
    }

    [BindProperty(SupportsGet = true)]
    public Guid? BranchId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? FromUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? ToUtc { get; set; }

    public FuelStatisticsDto Statistics { get; private set; } = null!;
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Statistics = await _analyticsService.GetFleetFuelStatisticsAsync(
            BranchId, VehicleId, FromUtc, ToUtc);

        Branches = await _branchService.GetBranchesAsync();
        var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 200 });
        Vehicles = vResult.Items;
    }
}
