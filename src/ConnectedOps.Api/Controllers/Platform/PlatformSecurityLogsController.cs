using ConnectedOps.Application.Platform;
using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/security-logs")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformSecurityLogsController : ControllerBase
{
    private readonly IPlatformSecurityService _securityService;

    public PlatformSecurityLogsController(IPlatformSecurityService securityService)
    {
        _securityService = securityService;
    }

    [HttpGet]
    public async Task<ActionResult<SecurityLogPage>> GetSecurityLogs(
        [FromQuery] PlatformSecurityLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _securityService.GetSecurityLogsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    public async Task<ActionResult<PlatformSecurityDashboardStatsDto>> GetStats(
        CancellationToken cancellationToken)
    {
        var result = await _securityService.GetSecurityDashboardStatsAsync(cancellationToken);
        return Ok(result);
    }
}
