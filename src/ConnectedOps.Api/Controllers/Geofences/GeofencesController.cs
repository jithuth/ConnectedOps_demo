using ConnectedOps.Application.Geofences;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Geofences;

[ApiController]
[Route("api/geofences")]
[Authorize]
public sealed class GeofencesController : ControllerBase
{
    private readonly IGeofenceService _geofenceService;

    public GeofencesController(IGeofenceService geofenceService)
    {
        _geofenceService = geofenceService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Geofences.View)]
    public async Task<IActionResult> GetGeofences(
        [FromQuery] GeofenceQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.GetGeofencesPagedAsync(parameters, cancellationToken);
        var count = await _geofenceService.GetGeofenceCountAsync(parameters, cancellationToken);
        return Ok(new { items = result, totalCount = count, page = parameters.Page, pageSize = parameters.PageSize });
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Geofences.View)]
    public async Task<IActionResult> GetGeofenceById(Guid id, CancellationToken cancellationToken)
    {
        var geofence = await _geofenceService.GetGeofenceByIdAsync(id, cancellationToken);
        if (geofence == null)
            return NotFound(new { error = $"Geofence '{id}' was not found." });

        return Ok(geofence);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Geofences.Create)]
    public async Task<IActionResult> CreateGeofence(
        [FromBody] CreateGeofenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.CreateGeofenceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetGeofenceById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Geofences.Edit)]
    public async Task<IActionResult> UpdateGeofence(
        Guid id,
        [FromBody] UpdateGeofenceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.UpdateGeofenceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.Geofences.Edit)]
    public async Task<IActionResult> ActivateGeofence(Guid id, CancellationToken cancellationToken)
    {
        var success = await _geofenceService.ActivateGeofenceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { error = $"Geofence '{id}' was not found." });

        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionKeys.Geofences.Edit)]
    public async Task<IActionResult> DeactivateGeofence(Guid id, CancellationToken cancellationToken)
    {
        var success = await _geofenceService.DeactivateGeofenceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { error = $"Geofence '{id}' was not found." });

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Geofences.Delete)]
    public async Task<IActionResult> DeleteGeofence(Guid id, CancellationToken cancellationToken)
    {
        var success = await _geofenceService.DeleteGeofenceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { error = $"Geofence '{id}' was not found." });

        return NoContent();
    }

    [HttpGet("{id:guid}/vehicles")]
    [RequirePermission(PermissionKeys.Geofences.View)]
    public async Task<IActionResult> GetVehiclesInsideGeofence(Guid id, CancellationToken cancellationToken)
    {
        var result = await _geofenceService.GetVehiclesInsideGeofenceAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.Geofences.View)]
    public async Task<IActionResult> GetGeofencesForVehicle(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await _geofenceService.GetGeofencesForVehicleAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("events")]
    [RequirePermission(PermissionKeys.Geofences.ViewEvents)]
    public async Task<IActionResult> GetGeofenceEvents(
        [FromQuery] GeofenceEventQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _geofenceService.GetGeofenceEventsPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }
}
