using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Safety;

[ApiController]
[Route("api/safety/dashboard")]
[Authorize]
public sealed class SafetyDashboardController : ControllerBase
{
    private readonly ISafetyDashboardService _dashboardService;

    public SafetyDashboardController(ISafetyDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.SafetyDashboard.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }
}
