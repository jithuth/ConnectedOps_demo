using ConnectedOps.Application.Optimization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Optimization;

[ApiController]
[Route("api/dispatch/optimization")]
[Authorize]
public sealed class RouteOptimizationController : ControllerBase
{
    private readonly IRouteOptimizationService _optimizationService;

    public RouteOptimizationController(IRouteOptimizationService optimizationService)
    {
        _optimizationService = optimizationService;
    }

    [HttpGet("runs")]
    [RequirePermission(PermissionKeys.RouteOptimization.View)]
    public async Task<IActionResult> GetRuns([FromQuery] OptimizationFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _optimizationService.GetOptimizationRunsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("runs/{runId:guid}")]
    [RequirePermission(PermissionKeys.RouteOptimization.View)]
    public async Task<IActionResult> GetRunById(Guid runId, CancellationToken cancellationToken)
    {
        var result = await _optimizationService.GetOptimizationRunByIdAsync(runId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost("solve")]
    [RequirePermission(PermissionKeys.RouteOptimization.RunOptimization)]
    public async Task<IActionResult> SolveVrp([FromBody] CreateOptimizationRunRequest request, CancellationToken cancellationToken)
    {
        var result = await _optimizationService.SolveVrpAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("dispatch")]
    [RequirePermission(PermissionKeys.RouteOptimization.DispatchRoute)]
    public async Task<IActionResult> DispatchRun([FromBody] DispatchOptimizationRunRequest request, CancellationToken cancellationToken)
    {
        var result = await _optimizationService.DispatchRunAsync(request, cancellationToken);
        return Ok(result);
    }
}
