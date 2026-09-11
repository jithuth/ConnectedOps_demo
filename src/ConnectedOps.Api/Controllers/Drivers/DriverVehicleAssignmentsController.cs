using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/driver-vehicle-assignments")]
[Authorize]
public sealed class DriverVehicleAssignmentsController : ControllerBase
{
    private readonly IDriverAssignmentService _assignmentService;
    private readonly IDriverEligibilityService _eligibilityService;

    public DriverVehicleAssignmentsController(
        IDriverAssignmentService assignmentService,
        IDriverEligibilityService eligibilityService)
    {
        _assignmentService = assignmentService;
        _eligibilityService = eligibilityService;
    }

    [HttpGet("active")]
    [RequirePermission(PermissionKeys.DriverAssignments.View)]
    public async Task<IActionResult> GetAllActiveAssignments(CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetAllActiveAssignmentsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("driver/{driverId:guid}")]
    [RequirePermission(PermissionKeys.DriverAssignments.View)]
    public async Task<IActionResult> GetDriverAssignments(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetDriverAssignmentsAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicle/{vehicleId:guid}")]
    [RequirePermission(PermissionKeys.DriverAssignments.View)]
    public async Task<IActionResult> GetVehicleAssignments(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.GetVehicleAssignmentsAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("eligibility")]
    [RequirePermission(PermissionKeys.DriverAssignments.View)]
    public async Task<IActionResult> EvaluateEligibility(
        [FromQuery] Guid driverId,
        [FromQuery] Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _eligibilityService.EvaluateAsync(driverId, vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.DriverAssignments.Create)]
    public async Task<IActionResult> CreateAssignment(
        [FromBody] CreateDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.CreateAssignmentAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/end")]
    [RequirePermission(PermissionKeys.DriverAssignments.End)]
    public async Task<IActionResult> EndAssignment(
        Guid id,
        [FromBody] EndDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _assignmentService.EndAssignmentAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
