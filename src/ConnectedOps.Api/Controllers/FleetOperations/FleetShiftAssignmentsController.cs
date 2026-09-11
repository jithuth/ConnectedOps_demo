using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/shift-assignments")]
[Authorize]
public sealed class FleetShiftAssignmentsController : ControllerBase
{
    private readonly IFleetShiftAssignmentService _assignmentService;

    public FleetShiftAssignmentsController(IFleetShiftAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] DateOnly? date,
        [FromQuery] Guid? shiftId,
        [FromQuery] Guid? driverId,
        [FromQuery] Guid? vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetAssignmentsAsync(date, shiftId, driverId, vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.View)]
    public async Task<IActionResult> GetAssignmentById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetAssignmentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FleetOperations.AssignShifts)]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.CreateAssignmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAssignmentById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.AssignShifts)]
    public async Task<IActionResult> UpdateAssignment(
        Guid id,
        [FromBody] UpdateShiftAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.UpdateAssignmentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.FleetOperations.AssignShifts)]
    public async Task<IActionResult> ActivateAssignment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.ActivateAssignmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.FleetOperations.AssignShifts)]
    public async Task<IActionResult> CompleteAssignment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _assignmentService.CompleteAssignmentAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.FleetOperations.AssignShifts)]
    public async Task<IActionResult> CancelAssignment(
        Guid id,
        [FromBody] CancelAssignmentApiRequest? request,
        CancellationToken cancellationToken)
    {
        await _assignmentService.CancelAssignmentAsync(id, request?.Reason, cancellationToken);
        return NoContent();
    }
}

public sealed record CancelAssignmentApiRequest(string? Reason);
