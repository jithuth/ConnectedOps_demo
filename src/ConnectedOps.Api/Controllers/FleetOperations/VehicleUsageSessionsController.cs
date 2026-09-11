using ConnectedOps.Application.FleetOperations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.FleetOperations;

[ApiController]
[Route("api/fleet-operations/sessions")]
[Authorize]
public sealed class VehicleUsageSessionsController : ControllerBase
{
    private readonly IVehicleUsageSessionService _sessionService;

    public VehicleUsageSessionsController(IVehicleUsageSessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetSessionsPaged(
        [FromQuery] UsageSessionQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSessionsPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetSessionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetSessionByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicle/{vehicleId:guid}/active")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetActiveSessionForVehicle(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetActiveSessionForVehicleAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("driver/{driverId:guid}/active")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetActiveSessionForDriver(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.GetActiveSessionForDriverAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicle/{vehicleId:guid}/history")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetVehicleUsageHistory(
        Guid vehicleId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetVehicleUsageHistoryAsync(vehicleId, limit, cancellationToken);
        return Ok(result);
    }

    [HttpGet("driver/{driverId:guid}/history")]
    [RequirePermission(PermissionKeys.FleetOperations.ViewSessions)]
    public async Task<IActionResult> GetDriverUsageHistory(
        Guid driverId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _sessionService.GetDriverUsageHistoryAsync(driverId, limit, cancellationToken);
        return Ok(result);
    }

    [HttpPost("checkout")]
    [RequirePermission(PermissionKeys.FleetOperations.CheckOut)]
    public async Task<IActionResult> CheckoutVehicle(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.CheckoutVehicleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSessionById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/checkin")]
    [RequirePermission(PermissionKeys.FleetOperations.CheckIn)]
    public async Task<IActionResult> CheckInVehicle(
        Guid id,
        [FromBody] CheckInSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sessionService.CheckInVehicleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.FleetOperations.ManageOperations)]
    public async Task<IActionResult> CancelSession(
        Guid id,
        [FromBody] CancelSessionApiRequest? request,
        CancellationToken cancellationToken)
    {
        await _sessionService.CancelSessionAsync(id, request?.Reason, cancellationToken);
        return NoContent();
    }
}

public sealed record CancelSessionApiRequest(string? Reason);
