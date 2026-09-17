using ConnectedOps.Application.Hos;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Hos;

[ApiController]
[Route("api/hos")]
[Authorize]
public sealed class HosController : ControllerBase
{
    private readonly IHosService _hosService;

    public HosController(IHosService hosService)
    {
        _hosService = hosService;
    }

    [HttpGet("clocks/{driverId:guid}")]
    [RequirePermission(PermissionKeys.Hos.View)]
    public async Task<IActionResult> GetClocks(Guid driverId, CancellationToken cancellationToken)
    {
        var result = await _hosService.GetDriverClocksAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("policy")]
    [RequirePermission(PermissionKeys.Hos.View)]
    public async Task<IActionResult> GetPolicy(CancellationToken cancellationToken)
    {
        var result = await _hosService.GetPolicyAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("policy")]
    [RequirePermission(PermissionKeys.Hos.ConfigurePolicy)]
    public async Task<IActionResult> UpdatePolicy([FromBody] UpdateHosPolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _hosService.UpdatePolicyAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("duty-status/{driverId:guid}")]
    [RequirePermission(PermissionKeys.Hos.LogDuty)]
    public async Task<IActionResult> ChangeDutyStatus(Guid driverId, [FromBody] ChangeDutyStatusRequest request, CancellationToken cancellationToken)
    {
        var result = await _hosService.ChangeDutyStatusAsync(driverId, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("logs")]
    [RequirePermission(PermissionKeys.Hos.View)]
    public async Task<IActionResult> GetLogs([FromQuery] HosLogFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _hosService.GetLogsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("violations")]
    [RequirePermission(PermissionKeys.Hos.ViewViolations)]
    public async Task<IActionResult> GetViolations([FromQuery] HosViolationFilterRequest request, CancellationToken cancellationToken)
    {
        var result = await _hosService.GetViolationsPagedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("violations/{id:guid}/acknowledge")]
    [RequirePermission(PermissionKeys.Hos.ViewViolations)]
    public async Task<IActionResult> AcknowledgeViolation(Guid id, CancellationToken cancellationToken)
    {
        var result = await _hosService.AcknowledgeViolationAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roadside-report/{driverId:guid}")]
    [RequirePermission(PermissionKeys.Hos.RoadsideInspection)]
    public async Task<IActionResult> GetRoadsideReport(Guid driverId, [FromQuery] DateTime? dateUtc, CancellationToken cancellationToken = default)
    {
        var targetDate = dateUtc ?? DateTime.UtcNow;
        var result = await _hosService.GenerateRoadsideReportAsync(driverId, targetDate, cancellationToken);
        return Ok(result);
    }
}
