using ConnectedOps.Application.Geofences;
using ConnectedOps.Application.Organization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Maps;

[Authorize]
public sealed class GeofencesModel : PageModel
{
    private readonly IGeofenceService _geofenceService;
    private readonly IBranchService _branchService;

    public GeofencesModel(
        IGeofenceService geofenceService,
        IBranchService branchService)
    {
        _geofenceService = geofenceService;
        _branchService = branchService;
    }

    public IReadOnlyCollection<GeofenceListItemDto> Geofences { get; private set; } = [];
    public IReadOnlyCollection<BranchListItemDto> Branches { get; private set; } = [];

    [BindProperty(SupportsGet = true)]
    public GeofenceQueryParameters Filter { get; set; } = new();

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Geofences = await _geofenceService.GetGeofencesPagedAsync(Filter, ct);
        Branches = await _branchService.GetBranchesAsync(ct);
    }

    public async Task<IActionResult> OnGetListAsync([FromQuery] GeofenceQueryParameters filter)
    {
        var ct = HttpContext.RequestAborted;
        var list = await _geofenceService.GetGeofencesPagedAsync(filter, ct);
        return new JsonResult(list);
    }

    public async Task<IActionResult> OnGetGeofenceDetailsAsync(Guid id)
    {
        var ct = HttpContext.RequestAborted;
        var geofence = await _geofenceService.GetGeofenceByIdAsync(id, ct);
        if (geofence == null) return NotFound();
        return new JsonResult(geofence);
    }

    public async Task<IActionResult> OnGetPresenceAsync(Guid id)
    {
        var ct = HttpContext.RequestAborted;
        var presence = await _geofenceService.GetVehiclesInsideGeofenceAsync(id, ct);
        return new JsonResult(presence);
    }

    public async Task<IActionResult> OnGetEventsAsync([FromQuery] GeofenceEventQueryParameters filter)
    {
        var ct = HttpContext.RequestAborted;
        var events = await _geofenceService.GetGeofenceEventsPagedAsync(filter, ct);
        return new JsonResult(events.Items);
    }

    public async Task<IActionResult> OnPostCreateAsync([FromBody] CreateGeofenceRequest request)
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var created = await _geofenceService.CreateGeofenceAsync(request, ct);
            return new JsonResult(new { success = true, id = created.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostUpdateAsync(Guid id, [FromBody] UpdateGeofenceRequest request)
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var updated = await _geofenceService.UpdateGeofenceAsync(id, request, ct);
            return new JsonResult(new { success = true, id = updated.Id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(Guid id, [FromQuery] bool active)
    {
        var ct = HttpContext.RequestAborted;
        var success = active
            ? await _geofenceService.ActivateGeofenceAsync(id, ct)
            : await _geofenceService.DeactivateGeofenceAsync(id, ct);

        if (!success)
        {
            return BadRequest(new { errors = new[] { "Failed to change geofence active status" } });
        }
        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var ct = HttpContext.RequestAborted;
        var success = await _geofenceService.DeleteGeofenceAsync(id, ct);
        if (!success)
        {
            return BadRequest(new { errors = new[] { "Failed to delete geofence" } });
        }
        return new JsonResult(new { success = true });
    }
}
