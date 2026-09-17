using ConnectedOps.Application.Predictive;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Predictive;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Predictive;

[ApiController]
[Route("api/predictive")]
[Authorize]
public sealed class PredictiveMaintenanceController : ControllerBase
{
    private readonly IPredictiveMaintenanceService _predictiveService;

    public PredictiveMaintenanceController(IPredictiveMaintenanceService predictiveService)
    {
        _predictiveService = predictiveService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _predictiveService.GetFleetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("vehicles/{vehicleId:guid}/digital-twin")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.View)]
    public async Task<IActionResult> GetVehicleDigitalTwin(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await _predictiveService.GetVehicleDigitalTwinAsync(vehicleId, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("alerts")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.View)]
    public async Task<IActionResult> GetAlerts(
        [FromQuery] Guid? vehicleId,
        [FromQuery] PredictiveRiskLevel? riskLevel,
        [FromQuery] PredictiveRecommendationStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _predictiveService.GetPredictiveAlertsPagedAsync(vehicleId, riskLevel, status, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("diagnostics/run")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.RunDiagnostics)]
    public async Task<IActionResult> RunDiagnostics([FromBody] TriggerDiagnosticScanRequest request, CancellationToken cancellationToken)
    {
        var newAlerts = await _predictiveService.RunFleetDiagnosticEvaluationAsync(request.VehicleId, cancellationToken);
        return Ok(new { alertsGenerated = newAlerts });
    }

    [HttpPost("alerts/{id:guid}/promote-work-order")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.GenerateWorkOrder)]
    public async Task<IActionResult> PromoteToWorkOrder(Guid id, [FromBody] PromoteToWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrderId = await _predictiveService.PromoteAlertToWorkOrderAsync(id, request, cancellationToken);
        return Ok(new { workOrderId });
    }

    [HttpPost("alerts/{id:guid}/dismiss")]
    [RequirePermission(PermissionKeys.PredictiveMaintenance.Dismiss)]
    public async Task<IActionResult> DismissAlert(Guid id, [FromQuery] string reason, CancellationToken cancellationToken)
    {
        var result = await _predictiveService.DismissAlertAsync(id, reason, cancellationToken);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
