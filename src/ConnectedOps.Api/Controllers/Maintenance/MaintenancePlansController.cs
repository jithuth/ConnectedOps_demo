using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maintenance;

[ApiController]
[Route("api/maintenance/plans")]
[Authorize]
public sealed class MaintenancePlansController : ControllerBase
{
    private readonly IMaintenancePlanService _planService;

    public MaintenancePlansController(IMaintenancePlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.MaintenancePlans.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        [FromQuery] Guid? vehicleCategoryId,
        CancellationToken cancellationToken)
    {
        var result = await _planService.GetAllAsync(activeOnly, vehicleCategoryId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _planService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.MaintenancePlans.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenancePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMaintenancePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _planService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // Rules
    [HttpPost("{planId:guid}/rules")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> AddRule(
        Guid planId,
        [FromBody] CreateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.AddRuleAsync(planId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{planId:guid}/rules/{ruleId:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> UpdateRule(
        Guid planId,
        Guid ruleId,
        [FromBody] UpdateMaintenancePlanRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.UpdateRuleAsync(planId, ruleId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{planId:guid}/rules/{ruleId:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> DeleteRule(
        Guid planId,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        await _planService.DeleteRuleAsync(planId, ruleId, cancellationToken);
        return NoContent();
    }

    // Vehicle Plan Assignments
    [HttpGet("assignments")]
    [RequirePermission(PermissionKeys.MaintenancePlans.View)]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] Guid? vehicleId,
        [FromQuery] Guid? planId,
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _planService.GetVehicleAssignmentsAsync(vehicleId, planId, activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpPost("assignments")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> AssignPlan(
        [FromBody] AssignVehiclePlanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.AssignPlanToVehicleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("assignments/{assignmentId:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> UpdateAssignment(
        Guid assignmentId,
        [FromBody] UpdateVehiclePlanAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _planService.UpdateVehicleAssignmentAsync(assignmentId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("assignments/{assignmentId:guid}")]
    [RequirePermission(PermissionKeys.MaintenancePlans.Edit)]
    public async Task<IActionResult> RemoveAssignment(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        await _planService.RemoveVehicleAssignmentAsync(assignmentId, cancellationToken);
        return NoContent();
    }
}
