using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets/dashboard")]
[Authorize]
public sealed class AssetDashboardController : ControllerBase
{
    private readonly IAssetDashboardService _dashboardService;
    private readonly IAssetUtilizationService _utilizationService;

    public AssetDashboardController(
        IAssetDashboardService dashboardService,
        IAssetUtilizationService utilizationService)
    {
        _dashboardService = dashboardService;
        _utilizationService = utilizationService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.AssetDashboard.View)]
    public async Task<IActionResult> GetSummary(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardSummaryAsync(branchId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("utilization")]
    [RequirePermission(PermissionKeys.AssetDashboard.View)]
    public async Task<IActionResult> GetUtilization(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _utilizationService.GetUtilizationSummaryAsync(branchId, categoryId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("idle")]
    [RequirePermission(PermissionKeys.AssetDashboard.View)]
    public async Task<IActionResult> GetIdleAssets(
        [FromQuery] int idleDaysThreshold = 30,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _utilizationService.GetIdleAssetsAsync(idleDaysThreshold, branchId, cancellationToken);
        return Ok(result);
    }
}
