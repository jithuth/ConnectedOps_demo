using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/telematics/dashboard")]
[Authorize]
public sealed class TelematicsDashboardController : ControllerBase
{
    private readonly ITelematicsDashboardService _dashboardService;

    public TelematicsDashboardController(ITelematicsDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Telematics.ViewDashboard)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }
}
