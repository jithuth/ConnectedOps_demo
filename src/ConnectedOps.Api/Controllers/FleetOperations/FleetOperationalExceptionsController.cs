using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/exceptions")]
[Authorize]
public sealed class FleetOperationalExceptionsController : ControllerBase
{
    private readonly IFleetOperationalExceptionService _exceptionService;

    public FleetOperationalExceptionsController(IFleetOperationalExceptionService exceptionService)
    {
        _exceptionService = exceptionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.ViewExceptions)]
    public async Task<IActionResult> GetExceptionsPaged(
        [FromQuery] OperationalExceptionQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.GetExceptionsPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewExceptions)]
    public async Task<IActionResult> GetExceptionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.GetExceptionByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FleetOperations.ManageExceptions)]
    public async Task<IActionResult> CreateException(
        [FromBody] CreateOperationalExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.CreateExceptionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetExceptionById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.FleetOperations.ResolveExceptions)]
    public async Task<IActionResult> ResolveException(
        Guid id,
        [FromBody] ResolveOperationalExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.ResolveExceptionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/dismiss")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageExceptions)]
    public async Task<IActionResult> DismissException(
        Guid id,
        [FromBody] DismissOperationalExceptionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _exceptionService.DismissExceptionAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
