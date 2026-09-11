using ConnectedOps.Application.Maps;
using ConnectedOps.Application.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maps;

[Authorize]
public sealed class HistoryModel : PageModel
{
    private readonly IFleetMapService _fleetMapService;
    private readonly IVehicleService _vehicleService;

    public HistoryModel(
        IFleetMapService fleetMapService,
        IVehicleService vehicleService)
    {
        _fleetMapService = fleetMapService;
        _vehicleService = vehicleService;
    }

    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public Guid? VehicleId { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? StartUtc { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? EndUtc { get; set; }

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        var pagedVehicles = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 200 }, ct);
        Vehicles = pagedVehicles.Items;

        StartUtc ??= DateTime.UtcNow.Date.AddDays(-1);
        EndUtc ??= DateTime.UtcNow;
    }

    public async Task<IActionResult> OnGetRouteDataAsync(Guid vehicleId, DateTime? startUtc, DateTime? endUtc, int maxPoints = 500)
    {
        var ct = HttpContext.RequestAborted;
        var start = startUtc ?? DateTime.UtcNow.Date.AddDays(-1);
        var end = endUtc ?? DateTime.UtcNow;

        var trail = await _fleetMapService.GetVehicleTrailAsync(
            vehicleId,
            new FleetMapTrailQueryParameters
            {
                MaxPoints = maxPoints,
                FromUtc = start,
                ToUtc = end
            },
            ct);

        var details = await _fleetMapService.GetVehicleMapStateAsync(vehicleId, ct);

        return new JsonResult(new
        {
            vehicle = details,
            trail = trail,
            pointCount = trail.Count
        });
    }
}
