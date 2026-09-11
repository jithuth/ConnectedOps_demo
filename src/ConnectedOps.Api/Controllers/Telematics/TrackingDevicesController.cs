using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/tracking-devices")]
[Authorize]
public sealed class TrackingDevicesController : ControllerBase
{
    private readonly ITrackingDeviceService _deviceService;
    private readonly IDeviceProvisioningService _provisioningService;
    private readonly IDeviceAssignmentService _assignmentService;
    private readonly IDeviceHealthService _healthService;
    private readonly ITelemetryHistoryService _historyService;

    public TrackingDevicesController(
        ITrackingDeviceService deviceService,
        IDeviceProvisioningService provisioningService,
        IDeviceAssignmentService assignmentService,
        IDeviceHealthService healthService,
        ITelemetryHistoryService historyService)
    {
        _deviceService = deviceService;
        _provisioningService = provisioningService;
        _assignmentService = assignmentService;
        _healthService = healthService;
        _historyService = historyService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetDevicesPaged(
        [FromQuery] TrackingDeviceQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var items = await _deviceService.GetDevicesPagedAsync(parameters, cancellationToken);
        var total = await _deviceService.GetDeviceCountAsync(parameters, cancellationToken);
        return Ok(new { items, totalCount = total, page = parameters.Page, pageSize = parameters.PageSize });
    }

    [HttpGet("available")]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetAvailableDevices(CancellationToken cancellationToken)
    {
        var items = await _deviceService.GetAvailableDevicesForAssignmentAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetDeviceById(Guid id, CancellationToken cancellationToken)
    {
        var device = await _deviceService.GetDeviceByIdAsync(id, cancellationToken);
        if (device == null)
            return NotFound(new { message = "Tracking device not found." });

        return Ok(device);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.TrackingDevices.Create)]
    public async Task<IActionResult> CreateDevice(
        [FromBody] CreateTrackingDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.CreateDeviceAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetDeviceById), new { id = device.Id }, device);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.TrackingDevices.Edit)]
    public async Task<IActionResult> UpdateDevice(
        Guid id,
        [FromBody] UpdateTrackingDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var device = await _deviceService.UpdateDeviceAsync(id, request, cancellationToken);
        return Ok(device);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.TrackingDevices.Edit)]
    public async Task<IActionResult> ActivateDevice(Guid id, CancellationToken cancellationToken)
    {
        var success = await _deviceService.ActivateDeviceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { message = "Tracking device not found." });

        return Ok(new { success = true });
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionKeys.TrackingDevices.Edit)]
    public async Task<IActionResult> DeactivateDevice(Guid id, CancellationToken cancellationToken)
    {
        var success = await _deviceService.DeactivateDeviceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { message = "Tracking device not found." });

        return Ok(new { success = true });
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.TrackingDevices.Delete)]
    public async Task<IActionResult> DeleteDevice(Guid id, CancellationToken cancellationToken)
    {
        var success = await _deviceService.DeleteDeviceAsync(id, cancellationToken);
        if (!success)
            return NotFound(new { message = "Tracking device not found." });

        return NoContent();
    }

    [HttpPost("{id:guid}/provision")]
    [RequirePermission(PermissionKeys.TrackingDevices.Provision)]
    public async Task<IActionResult> ProvisionDevice(
        Guid id,
        [FromBody] ProvisionDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _provisioningService.ProvisionDeviceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/deprovision")]
    [RequirePermission(PermissionKeys.TrackingDevices.Provision)]
    public async Task<IActionResult> DeprovisionDevice(
        Guid id,
        [FromBody] DeprovisionDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _provisioningService.DeprovisionDeviceAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/health")]
    [RequirePermission(PermissionKeys.Telematics.ViewDeviceHealth)]
    public async Task<IActionResult> GetDeviceHealth(Guid id, CancellationToken cancellationToken)
    {
        var health = await _healthService.GetDeviceHealthByIdAsync(id, cancellationToken);
        if (health == null)
            return NotFound(new { message = "Tracking device not found." });

        return Ok(health);
    }

    [HttpGet("{id:guid}/assignment-history")]
    [RequirePermission(PermissionKeys.TrackingDevices.View)]
    public async Task<IActionResult> GetAssignmentHistory(Guid id, CancellationToken cancellationToken)
    {
        var history = await _assignmentService.GetAssignmentHistoryForDeviceAsync(id, cancellationToken);
        return Ok(history);
    }

    [HttpGet("{id:guid}/telemetry")]
    [RequirePermission(PermissionKeys.Telematics.ViewHistory)]
    public async Task<IActionResult> GetDeviceTelemetry(
        Guid id,
        [FromQuery] TelemetryHistoryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var history = await _historyService.GetDeviceTelemetryHistoryAsync(id, parameters, cancellationToken);
        return Ok(history);
    }
}
