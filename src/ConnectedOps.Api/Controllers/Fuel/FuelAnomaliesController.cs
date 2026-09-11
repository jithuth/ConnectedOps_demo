using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Route("api/fuel/anomalies")]
[Authorize]
public sealed class FuelAnomaliesController : ControllerBase
{
    private readonly IFuelAnomalyService _anomalyService;

    public FuelAnomaliesController(IFuelAnomalyService anomalyService)
    {
        _anomalyService = anomalyService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FuelAnomalies.View)]
    public async Task<IActionResult> GetAnomalies(
        [FromQuery] FuelAnomalyQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _anomalyService.GetAnomaliesPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost("evaluate/{transactionId:guid}")]
    [RequirePermission(PermissionKeys.FuelAnomalies.View)]
    public async Task<IActionResult> EvaluateTransaction(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var result = await _anomalyService.EvaluateTransactionAnomaliesAsync(transactionId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.FuelAnomalies.Resolve)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveFuelAnomalyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _anomalyService.ResolveAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/dismiss")]
    [RequirePermission(PermissionKeys.FuelAnomalies.Resolve)]
    public async Task<IActionResult> Dismiss(
        Guid id,
        [FromBody] DismissFuelAnomalyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _anomalyService.DismissAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
