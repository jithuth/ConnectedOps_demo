using ConnectedOps.Application.ColdChain;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.ColdChain;

[ApiController]
[Route("api/cold-chain")]
[Authorize]
public sealed class ColdChainController : ControllerBase
{
    private readonly IColdChainService _coldChainService;

    public ColdChainController(IColdChainService coldChainService)
    {
        _coldChainService = coldChainService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.ColdChain.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _coldChainService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("sensors")]
    [RequirePermission(PermissionKeys.ColdChain.View)]
    public async Task<IActionResult> GetSensors(
        [FromQuery] SensorFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _coldChainService.GetSensorsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sensors")]
    [RequirePermission(PermissionKeys.ColdChain.ManageSensors)]
    public async Task<IActionResult> RegisterSensor(
        [FromBody] RegisterCargoSensorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coldChainService.RegisterSensorAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("telemetry")]
    [RequirePermission(PermissionKeys.ColdChain.ManageSensors)]
    public async Task<IActionResult> RecordTelemetry(
        [FromBody] RecordCargoTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coldChainService.RecordTelemetryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("excursions")]
    [RequirePermission(PermissionKeys.ColdChain.ViewExcursions)]
    public async Task<IActionResult> GetExcursions(
        [FromQuery] ExcursionFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _coldChainService.GetExcursionsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost("excursions/{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.ColdChain.ManageSensors)]
    public async Task<IActionResult> ResolveExcursion(
        Guid id,
        [FromBody] ResolveExcursionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _coldChainService.ResolveExcursionAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
