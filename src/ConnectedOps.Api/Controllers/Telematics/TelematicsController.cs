using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/telematics")]
[Authorize]
public sealed class TelematicsController : ControllerBase
{
    private readonly ITelemetryHistoryService _historyService;
    private readonly IDeviceHealthService _healthService;
    private readonly IDeviceCommandService _commandService;

    public TelematicsController(
        ITelemetryHistoryService historyService,
        IDeviceHealthService healthService,
        IDeviceCommandService commandService)
    {
        _historyService = historyService;
        _healthService = healthService;
        _commandService = commandService;
    }

    [HttpGet("vehicle/{vehicleId:guid}/history")]
    [RequirePermission(PermissionKeys.Telematics.ViewHistory)]
    public async Task<IActionResult> GetVehicleTelemetryHistory(
        Guid vehicleId,
        [FromQuery] TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _historyService.GetVehicleTelemetryHistoryAsync(vehicleId, parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("health")]
    [RequirePermission(PermissionKeys.Telematics.ViewDeviceHealth)]
    public async Task<IActionResult> GetDeviceHealthPaged(
        [FromQuery] DeviceHealthQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var items = await _healthService.GetDeviceHealthPagedAsync(parameters, cancellationToken);
        var total = await _healthService.GetDeviceHealthCountAsync(parameters, cancellationToken);
        return Ok(new { items, totalCount = total, page = parameters.Page, pageSize = parameters.PageSize });
    }

    [HttpPost("devices/{deviceId:guid}/commands")]
    [RequirePermission(PermissionKeys.Telematics.SendCommands)]
    public async Task<IActionResult> SendDeviceCommand(
        Guid deviceId,
        [FromBody] CreateDeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        var command = await _commandService.SendCommandAsync(deviceId, request, cancellationToken);
        return Ok(command);
    }

    [HttpGet("commands/{commandId:guid}")]
    [RequirePermission(PermissionKeys.Telematics.ViewDeviceHealth)]
    public async Task<IActionResult> GetCommand(
        Guid commandId,
        CancellationToken cancellationToken)
    {
        var command = await _commandService.GetCommandByIdAsync(commandId, cancellationToken);
        if (command == null)
            return NotFound(new { message = "Device command not found." });

        return Ok(command);
    }

    [HttpPost("commands/{commandId:guid}/cancel")]
    [RequirePermission(PermissionKeys.Telematics.SendCommands)]
    public async Task<IActionResult> CancelCommand(
        Guid commandId,
        [FromBody] CancelDeviceCommandRequest request,
        CancellationToken cancellationToken)
    {
        var command = await _commandService.CancelCommandAsync(commandId, request, cancellationToken);
        return Ok(command);
    }

    [HttpGet("devices/{deviceId:guid}/commands")]
    [RequirePermission(PermissionKeys.Telematics.ViewDeviceHealth)]
    public async Task<IActionResult> GetDeviceCommands(
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var commands = await _commandService.GetCommandsForDeviceAsync(deviceId, cancellationToken);
        return Ok(commands);
    }
}
