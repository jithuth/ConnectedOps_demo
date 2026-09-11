using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maintenance;

[ApiController]
[Authorize]
public sealed class MaintenanceDueController : ControllerBase
{
    private readonly IMaintenanceScheduleService _scheduleService;

    public MaintenanceDueController(IMaintenanceScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpGet("api/maintenance/due")]
    [RequirePermission(PermissionKeys.MaintenanceDue.View)]
    public async Task<IActionResult> GetDueMaintenance(
        [FromQuery] MaintenanceDueQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _scheduleService.GetDueMaintenanceAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/vehicles/{vehicleId:guid}/maintenance/due")]
    [RequirePermission(PermissionKeys.MaintenanceDue.View)]
    public async Task<IActionResult> GetVehicleDueMaintenance(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _scheduleService.CalculateVehicleMaintenanceAsync(vehicleId, cancellationToken);
        return Ok(result);
    }
}
