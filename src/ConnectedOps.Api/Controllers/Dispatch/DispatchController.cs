using ConnectedOps.Application.Dispatch;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Dispatch;

[ApiController]
[Route("api/dispatch")]
[Authorize]
public sealed class DispatchController : ControllerBase
{
    private readonly IDispatchService _dispatchService;

    public DispatchController(IDispatchService dispatchService)
    {
        _dispatchService = dispatchService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.Dispatch.ViewDashboard)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    // =========================================================================
    // JOBS
    // =========================================================================
    [HttpGet("jobs")]
    [RequirePermission(PermissionKeys.Dispatch.ViewJobs)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] DispatchJobFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetJobsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("jobs/{id:guid}")]
    [RequirePermission(PermissionKeys.Dispatch.ViewJobs)]
    public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetJobByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("jobs")]
    [RequirePermission(PermissionKeys.Dispatch.CreateJob)]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateDispatchJobRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.CreateJobAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetJobById), new { id = result.Id }, result);
    }

    [HttpPut("jobs/{id:guid}")]
    [RequirePermission(PermissionKeys.Dispatch.EditJob)]
    public async Task<IActionResult> UpdateJob(
        Guid id,
        [FromBody] UpdateDispatchJobRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.UpdateJobAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("jobs/{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.Dispatch.CancelJob)]
    public async Task<IActionResult> CancelJob(
        Guid id,
        [FromQuery] string? reason,
        CancellationToken cancellationToken)
    {
        var success = await _dispatchService.CancelJobAsync(id, reason, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    // =========================================================================
    // ROUTES
    // =========================================================================
    [HttpGet("routes")]
    [RequirePermission(PermissionKeys.Dispatch.ViewRoutes)]
    public async Task<IActionResult> GetRoutes(
        [FromQuery] DispatchRouteFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetRoutesPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("routes/{id:guid}")]
    [RequirePermission(PermissionKeys.Dispatch.ViewRoutes)]
    public async Task<IActionResult> GetRouteById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetRouteByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("routes")]
    [RequirePermission(PermissionKeys.Dispatch.CreateRoute)]
    public async Task<IActionResult> CreateRoute(
        [FromBody] CreateDispatchRouteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.CreateRouteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetRouteById), new { id = result.Id }, result);
    }

    [HttpPut("routes/{id:guid}")]
    [RequirePermission(PermissionKeys.Dispatch.EditRoute)]
    public async Task<IActionResult> UpdateRoute(
        Guid id,
        [FromBody] UpdateDispatchRouteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.UpdateRouteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("routes/{id:guid}/dispatch")]
    [RequirePermission(PermissionKeys.Dispatch.DispatchRoute)]
    public async Task<IActionResult> DispatchRoute(Guid id, CancellationToken cancellationToken)
    {
        var success = await _dispatchService.DispatchRouteAsync(id, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("routes/{id:guid}/start")]
    [RequirePermission(PermissionKeys.Dispatch.DispatchRoute)]
    public async Task<IActionResult> StartRoute(Guid id, CancellationToken cancellationToken)
    {
        var success = await _dispatchService.StartRouteAsync(id, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("routes/{id:guid}/complete")]
    [RequirePermission(PermissionKeys.Dispatch.DispatchRoute)]
    public async Task<IActionResult> CompleteRoute(
        Guid id,
        [FromQuery] decimal? actualDistanceKm,
        CancellationToken cancellationToken)
    {
        var success = await _dispatchService.CompleteRouteAsync(id, actualDistanceKm, cancellationToken);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpPost("routes/{id:guid}/stops")]
    [RequirePermission(PermissionKeys.Dispatch.EditRoute)]
    public async Task<IActionResult> AddStop(
        Guid id,
        [FromBody] AddRouteStopRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.AddStopAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("routes/{id:guid}/stops/{stopId:guid}")]
    [RequirePermission(PermissionKeys.Dispatch.EditRoute)]
    public async Task<IActionResult> RemoveStop(
        Guid id,
        Guid stopId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.RemoveStopAsync(id, stopId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("routes/{id:guid}/reorder")]
    [RequirePermission(PermissionKeys.Dispatch.EditRoute)]
    public async Task<IActionResult> ReorderStops(
        Guid id,
        [FromBody] List<Guid> stopIdsInOrder,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.ReorderStopsAsync(id, stopIdsInOrder, cancellationToken);
        return Ok(result);
    }

    [HttpPost("routes/{id:guid}/optimize")]
    [RequirePermission(PermissionKeys.Dispatch.EditRoute)]
    public async Task<IActionResult> OptimizeRoute(
        Guid id,
        [FromBody] OptimizeRouteStopsRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.OptimizeRouteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // =========================================================================
    // STOPS & POD
    // =========================================================================
    [HttpPost("stops/{stopId:guid}/status")]
    [RequirePermission(PermissionKeys.Dispatch.ViewRoutes)]
    public async Task<IActionResult> UpdateStopStatus(
        Guid stopId,
        [FromBody] UpdateRouteStopStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.UpdateStopStatusAsync(stopId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("jobs/{jobId:guid}/pod")]
    [RequirePermission(PermissionKeys.Dispatch.CompletePod)]
    public async Task<IActionResult> RecordProofOfDelivery(
        Guid jobId,
        [FromBody] RecordProofOfDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.RecordProofOfDeliveryAsync(jobId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("jobs/{jobId:guid}/pod")]
    [RequirePermission(PermissionKeys.Dispatch.ViewJobs)]
    public async Task<IActionResult> GetProofOfDelivery(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var result = await _dispatchService.GetProofOfDeliveryByJobIdAsync(jobId, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }
}
