using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/dashboard")]
[Authorize]
public sealed class FleetOperationsDashboardController : ControllerBase
{
    private readonly IFleetOperationsDashboardService _dashboardService;
    private readonly IFleetActivityTimelineService _timelineService;

    public FleetOperationsDashboardController(
        IFleetOperationsDashboardService dashboardService,
        IFleetActivityTimelineService timelineService)
    {
        _dashboardService = dashboardService;
        _timelineService = timelineService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.ViewDashboard)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("board")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewBoard)]
    public async Task<IActionResult> GetBoard(
        [FromQuery] FleetAvailabilityFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetBoardAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("timeline")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewTimeline)]
    public async Task<IActionResult> GetTimeline(
        [FromQuery] FleetActivityQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _timelineService.GetActivityTimelinePagedAsync(parameters, cancellationToken);
        return Ok(result);
    }
}
