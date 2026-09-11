using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/tracking-device-assignments")]
[Authorize]
public sealed class TrackingDeviceAssignmentsController : ControllerBase
{
    private readonly IDeviceAssignmentService _assignmentService;

    public TrackingDeviceAssignmentsController(IDeviceAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.TrackingDevices.AssignVehicle)]
    public async Task<IActionResult> AssignDevice(
        [FromBody] AssignDeviceToVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentService.AssignDeviceToVehicleAsync(request, cancellationToken);
        return Ok(assignment);
    }

    [HttpPost("{id:guid}/end")]
    [RequirePermission(PermissionKeys.TrackingDevices.AssignVehicle)]
    public async Task<IActionResult> EndAssignment(
        Guid id,
        [FromBody] EndDeviceAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentService.EndDeviceAssignmentAsync(id, request, cancellationToken);
        return Ok(assignment);
    }

    [HttpGet("vehicle/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetActiveAssignmentForVehicle(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var assignment = await _assignmentService.GetActiveAssignmentForVehicleAsync(vehicleId, cancellationToken);
        if (assignment == null)
            return NotFound(new { message = "No active tracking device assignment found for this vehicle." });

        return Ok(assignment);
    }

    [HttpGet("vehicle/{vehicleId:guid}/history")]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetAssignmentHistoryForVehicle(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var history = await _assignmentService.GetAssignmentHistoryForVehicleAsync(vehicleId, cancellationToken);
        return Ok(history);
    }
}
