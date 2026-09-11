using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/drivers/dashboard")]
[Authorize]
public sealed class DriverDashboardController : ControllerBase
{
    private readonly IDriverDashboardService _dashboardService;

    public DriverDashboardController(IDriverDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.DriverDashboard.View)]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
