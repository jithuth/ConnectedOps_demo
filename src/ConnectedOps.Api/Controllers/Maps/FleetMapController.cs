using ConnectedOps.Application.Maps;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maps;

[ApiController]
[Route("api/maps")]
[Authorize]
public sealed class FleetMapController : ControllerBase
{
    private readonly IFleetMapService _fleetMapService;

    public FleetMapController(IFleetMapService fleetMapService)
    {
        _fleetMapService = fleetMapService;
    }

    [HttpGet("fleet")]
    [RequirePermission(PermissionKeys.Maps.View)]
    public async Task<IActionResult> GetFleet(
        [FromQuery] FleetMapQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetFleetMapVehiclesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("fleet/geojson")]
    [RequirePermission(PermissionKeys.Maps.View)]
    public async Task<IActionResult> GetFleetGeoJson(
        [FromQuery] FleetMapQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetFleetMapGeoJsonAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.Maps.View)]
    public async Task<IActionResult> GetVehicleMapState(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetVehicleMapStateAsync(vehicleId, cancellationToken);
        if (result == null)
            return NotFound(new { error = $"Vehicle '{vehicleId}' not found or has no active mapping state." });

        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}/trail")]
    [RequirePermission(PermissionKeys.Maps.ViewHistory)]
    public async Task<IActionResult> GetVehicleTrail(
        Guid vehicleId,
        [FromQuery] FleetMapTrailQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetVehicleTrailAsync(vehicleId, parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.Maps.View)]
    public async Task<IActionResult> GetDashboardMetrics(CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetMapDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("settings")]
    [RequirePermission(PermissionKeys.Maps.View)]
    public async Task<IActionResult> GetMapSettings(CancellationToken cancellationToken)
    {
        var result = await _fleetMapService.GetMapClientConfigurationAsync(cancellationToken);
        return Ok(result);
    }
}
