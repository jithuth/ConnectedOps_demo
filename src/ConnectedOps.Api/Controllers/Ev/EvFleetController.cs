using ConnectedOps.Application.Ev;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Ev;

[ApiController]
[Route("api/ev")]
[Authorize]
public sealed class EvFleetController : ControllerBase
{
    private readonly IEvService _evService;

    public EvFleetController(IEvService evService)
    {
        _evService = evService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.EvFleet.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _evService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("batteries")]
    [RequirePermission(PermissionKeys.EvFleet.View)]
    public async Task<IActionResult> GetBatteries([FromQuery] EvFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.GetVehicleBatteriesPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stations")]
    [RequirePermission(PermissionKeys.EvFleet.View)]
    public async Task<IActionResult> GetStations(CancellationToken cancellationToken)
    {
        var result = await _evService.GetStationsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("stations")]
    [RequirePermission(PermissionKeys.EvFleet.ManageStations)]
    public async Task<IActionResult> CreateStation([FromBody] CreateChargingStationRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.CreateStationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("telemetry")]
    [RequirePermission(PermissionKeys.EvFleet.View)]
    public async Task<IActionResult> UpdateTelemetry([FromBody] UpdateVehicleBatteryTelemetryRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.UpdateTelemetryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("smart-charging")]
    [RequirePermission(PermissionKeys.EvFleet.ScheduleCharging)]
    public async Task<IActionResult> ConfigureSmartCharging([FromBody] ConfigureSmartChargingRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.ConfigureSmartChargingAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions/start")]
    [RequirePermission(PermissionKeys.EvFleet.ControlSessions)]
    public async Task<IActionResult> StartSession([FromBody] StartChargingSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.StartSessionAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sessions/complete")]
    [RequirePermission(PermissionKeys.EvFleet.ControlSessions)]
    public async Task<IActionResult> CompleteSession([FromBody] CompleteChargingSessionRequest request, CancellationToken cancellationToken)
    {
        var result = await _evService.CompleteSessionAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("sessions")]
    [RequirePermission(PermissionKeys.EvFleet.View)]
    public async Task<IActionResult> GetSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _evService.GetSessionsPagedAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
