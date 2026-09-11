using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/shifts")]
[Authorize]
public sealed class FleetShiftsController : ControllerBase
{
    private readonly IFleetShiftService _shiftService;

    public FleetShiftsController(IFleetShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetShifts(
        [FromQuery] Guid? branchId,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _shiftService.GetShiftsAsync(branchId, isActive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetShiftById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _shiftService.GetShiftByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FleetOperations.ManageShifts)]
    public async Task<IActionResult> CreateShift(
        [FromBody] CreateFleetShiftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shiftService.CreateShiftAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetShiftById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageShifts)]
    public async Task<IActionResult> UpdateShift(
        Guid id,
        [FromBody] UpdateFleetShiftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _shiftService.UpdateShiftAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageShifts)]
    public async Task<IActionResult> ActivateShift(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _shiftService.ActivateShiftAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageShifts)]
    public async Task<IActionResult> DeactivateShift(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _shiftService.DeactivateShiftAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageShifts)]
    public async Task<IActionResult> DeleteShift(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _shiftService.DeleteShiftAsync(id, cancellationToken);
        return NoContent();
    }
}
