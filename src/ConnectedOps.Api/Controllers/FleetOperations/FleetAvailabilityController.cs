using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/availability")]
[Authorize]
public sealed class FleetAvailabilityController : ControllerBase
{
    private readonly IFleetAvailabilityService _availabilityService;

    public FleetAvailabilityController(IFleetAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetFleetAvailability(
        [FromQuery] FleetAvailabilityFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _availabilityService.GetFleetAvailabilityAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetVehicleAvailability(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _availabilityService.GetVehicleAvailabilityAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("drivers/{driverId:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetDriverAvailability(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _availabilityService.GetDriverAvailabilityAsync(driverId, cancellationToken);
        return Ok(result);
    }
}
