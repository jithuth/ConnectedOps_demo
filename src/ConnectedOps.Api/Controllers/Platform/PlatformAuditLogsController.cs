using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/audit-logs")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformAuditLogsController : ControllerBase
{
    private readonly IPlatformAuditService _auditService;

    public PlatformAuditLogsController(IPlatformAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<AuditLogPage>> GetAuditLogs(
        [FromQuery] PlatformAuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _auditService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(result);
    }
}
