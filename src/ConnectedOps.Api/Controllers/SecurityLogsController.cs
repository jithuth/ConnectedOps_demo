using ConnectedOps.Application.Security;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/security-logs")]
[Authorize]
public sealed class SecurityLogsController
    : ControllerBase
{
    private readonly ISecurityLogService
        _securityLogService;

    public SecurityLogsController(
        ISecurityLogService securityLogService)
    {
        _securityLogService =
            securityLogService;
    }

    [HttpGet]
    [RequirePermission(
        PermissionKeys.SecurityLogs.View)]
    public async Task<ActionResult<SecurityLogPage>>
        GetAsync(
            [FromQuery] SecurityLogQuery query,
            CancellationToken cancellationToken)
    {
        var result =
            await _securityLogService.GetAsync(
                query,
                cancellationToken);

        return Ok(result);
    }
}