using ConnectedOps.Application.Alerts;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Alerts;

public sealed record ResolveAlertBody(string? Notes = null);
public sealed record DismissAlertBody(string? Reason = null);

[ApiController]
[Route("api/alerts")]
[Authorize]
public sealed class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.AlertDashboard.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _alertService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Alerts.View)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] AlertFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.GetAlertsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Alerts.View)]
    public async Task<IActionResult> GetAlertById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _alertService.GetAlertByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("{id:guid}/acknowledge")]
    [RequirePermission(PermissionKeys.Alerts.Acknowledge)]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken cancellationToken)
    {
        var result = await _alertService.AcknowledgeAlertAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.Alerts.Resolve)]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] ResolveAlertBody body,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.ResolveAlertAsync(id, body?.Notes, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/dismiss")]
    [RequirePermission(PermissionKeys.Alerts.Dismiss)]
    public async Task<IActionResult> Dismiss(
        Guid id,
        [FromBody] DismissAlertBody body,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.DismissAlertAsync(id, body?.Reason, cancellationToken);
        return Ok(result);
    }

    [HttpPost("notifications")]
    [RequirePermission(PermissionKeys.Notifications.ViewTenantHistory)]
    public async Task<IActionResult> SendNotification(
        [FromBody] SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.SendNotificationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("rules")]
    [RequirePermission(PermissionKeys.AlertRules.View)]
    public async Task<IActionResult> GetRules(CancellationToken cancellationToken)
    {
        var result = await _alertService.GetRulesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("rules")]
    [RequirePermission(PermissionKeys.AlertRules.Create)]
    public async Task<IActionResult> CreateRule(
        [FromBody] CreateAlertRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.CreateRuleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("rules/{id:guid}")]
    [RequirePermission(PermissionKeys.AlertRules.Edit)]
    public async Task<IActionResult> UpdateRule(
        Guid id,
        [FromBody] UpdateAlertRuleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _alertService.UpdateRuleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
