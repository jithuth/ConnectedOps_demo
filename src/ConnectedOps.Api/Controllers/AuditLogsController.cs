using ConnectedOps.Application.Auditing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public sealed class AuditLogsController
    : ControllerBase
{
    private readonly IAuditLogService
        _auditLogService;

    public AuditLogsController(
        IAuditLogService auditLogService)
    {
        _auditLogService =
            auditLogService;
    }

    [HttpGet]
    [RequirePermission(
        PermissionKeys.AuditLogs.View)]
    public async Task<ActionResult<AuditLogPage>>
        GetAsync(
            [FromQuery] AuditLogQuery query,
            CancellationToken cancellationToken)
    {
        var result =
            await _auditLogService.GetAsync(
                query,
                cancellationToken);

        return Ok(result);
    }
}