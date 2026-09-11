using ConnectedOps.Application.Geofences;
using ConnectedOps.Application.Maps;
using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maps;

[Authorize]
public sealed class LiveModel : PageModel
{
    private readonly IFleetMapService _fleetMapService;
    private readonly IGeofenceService _geofenceService;
    private readonly IBranchService _branchService;

    public LiveModel(
        IFleetMapService fleetMapService,
        IGeofenceService geofenceService,
        IBranchService branchService)
    {
        _fleetMapService = fleetMapService;
        _geofenceService = geofenceService;
        _branchService = branchService;
    }

    public FleetMapDashboardDto Metrics { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];
    public IReadOnlyCollection<GeofenceListItemDto> Geofences { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public FleetMapQueryParameters Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Metrics = await _fleetMapService.GetMapDashboardMetricsAsync(ct);
        Branches = await _branchService.GetBranchesAsync(ct);
        Geofences = await _geofenceService.GetGeofencesPagedAsync(new GeofenceQueryParameters { IsActive = true, PageSize = 200 }, ct);
    }

    public async Task<IActionResult> OnGetFleetDataAsync([FromQuery] FleetMapQueryParameters filter)
    {
        var ct = HttpContext.RequestAborted;
        var vehicles = await _fleetMapService.GetFleetMapVehiclesAsync(filter, ct);
        return new JsonResult(vehicles);
    }

    public async Task<IActionResult> OnGetFleetGeoJsonAsync([FromQuery] FleetMapQueryParameters filter)
    {
        var ct = HttpContext.RequestAborted;
        var geoJson = await _fleetMapService.GetFleetMapGeoJsonAsync(filter, ct);
        return new JsonResult(geoJson);
    }

    public async Task<IActionResult> OnGetVehicleDetailsAsync(Guid id)
    {
        var ct = HttpContext.RequestAborted;
        var details = await _fleetMapService.GetVehicleMapStateAsync(id, ct);
        if (details == null) return NotFound();

        var geofenceMemberships = await _geofenceService.GetGeofencesForVehicleAsync(id, ct);

        return new JsonResult(new
        {
            vehicle = details,
            currentGeofences = geofenceMemberships
        });
    }

    public async Task<IActionResult> OnGetVehicleTrailAsync(Guid id, [FromQuery] int points = 100)
    {
        var ct = HttpContext.RequestAborted;
        var trail = await _fleetMapService.GetVehicleTrailAsync(id, new FleetMapTrailQueryParameters { MaxPoints = points }, ct);
        return new JsonResult(trail);
    }

    public async Task<IActionResult> OnGetGeofencesDataAsync()
    {
        var ct = HttpContext.RequestAborted;
        var geofences = await _geofenceService.GetGeofencesPagedAsync(new GeofenceQueryParameters { IsActive = true, PageSize = 200 }, ct);
        return new JsonResult(geofences);
    }

    public async Task<IActionResult> OnGetMetricsAsync()
    {
        var ct = HttpContext.RequestAborted;
        var metrics = await _fleetMapService.GetMapDashboardMetricsAsync(ct);
        return new JsonResult(metrics);
    }
}
