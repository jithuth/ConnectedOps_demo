using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Authorize]
public sealed class FuelAnalyticsController : ControllerBase
{
    private readonly IFuelAnalyticsService _analyticsService;

    public FuelAnalyticsController(IFuelAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("api/fuel/analytics/statistics")]
    [RequirePermission(PermissionKeys.FuelAnalytics.View)]
    public async Task<IActionResult> GetFleetStatistics(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? vehicleId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetFleetFuelStatisticsAsync(
            branchId, vehicleId, fromUtc, toUtc, cancellationToken);
        return Ok(result);
    }

    [HttpGet("api/vehicles/{vehicleId:guid}/fuel-summary")]
    [RequirePermission(PermissionKeys.FuelAnalytics.View)]
    public async Task<IActionResult> GetVehicleFuelSummary(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _analyticsService.GetVehicleFuelSummaryAsync(vehicleId, cancellationToken);
        return Ok(result);
    }
}
