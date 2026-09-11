using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Route("api/fuel/dashboard")]
[Authorize]
public sealed class FuelDashboardController : ControllerBase
{
    private readonly IFuelDashboardService _dashboardService;

    public FuelDashboardController(IFuelDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FuelDashboard.View)]
    public async Task<IActionResult> GetDashboardMetrics(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }
}
