using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Vehicles;

[ApiController]
[Route("api/vehicles/dashboard")]
[Authorize]
public sealed class VehicleDashboardController : ControllerBase
{
    private readonly IVehicleDashboardService _dashboardService;

    public VehicleDashboardController(IVehicleDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.VehicleDashboard.View)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        return Ok(result);
    }
}
