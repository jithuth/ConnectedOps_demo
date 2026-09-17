using ConnectedOps.Application.Integrations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Integrations;

[ApiController]
[Route("api/integrations/fuel-clearinghouse")]
[Authorize]
public sealed class FuelClearinghouseController : ControllerBase
{
    private readonly IFuelClearinghouseService _fuelService;

    public FuelClearinghouseController(IFuelClearinghouseService fuelService)
    {
        _fuelService = fuelService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _fuelService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("sync-logs")]
    [RequirePermission(PermissionKeys.Integrations.View)]
    public async Task<IActionResult> GetSyncLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await _fuelService.GetSyncLogsPagedAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sync")]
    [RequirePermission(PermissionKeys.Integrations.SyncFuelFeeds)]
    public async Task<IActionResult> TriggerSync([FromBody] TriggerFuelSyncRequest request, CancellationToken cancellationToken)
    {
        var result = await _fuelService.TriggerSyncAsync(request, cancellationToken);
        return Ok(result);
    }
}
