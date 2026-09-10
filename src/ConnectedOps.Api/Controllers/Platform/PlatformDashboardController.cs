using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/dashboard")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformDashboardController : ControllerBase
{
    private readonly IPlatformDashboardService _dashboardService;

    public PlatformDashboardController(IPlatformDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformDashboardStatsDto>> GetDashboardStats(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardStatsAsync(cancellationToken);
        return Ok(result);
    }
}
