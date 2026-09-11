using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/handovers")]
[Authorize]
public sealed class VehicleHandoversController : ControllerBase
{
    private readonly IVehicleHandoverService _handoverService;

    public VehicleHandoversController(IVehicleHandoverService handoverService)
    {
        _handoverService = handoverService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetHandovers(
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? driverId,
        CancellationToken cancellationToken)
    {
        var result = await _handoverService.GetHandoversAsync(vehicleId, driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetHandoverById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _handoverService.GetHandoverByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FleetOperations.Handover)]
    public async Task<IActionResult> CreateHandover(
        [FromBody] CreateVehicleHandoverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handoverService.CreateHandoverAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetHandoverById), new { id = result.Id }, result);
    }
}
