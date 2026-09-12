using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Compliance;

[ApiController]
[Route("api/compliance/dashboard")]
[Authorize]
public sealed class ComplianceDashboardController : ControllerBase
{
    private readonly IComplianceDashboardService _dashboardService;

    public ComplianceDashboardController(IComplianceDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.ComplianceDashboard.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardMetricsAsync(cancellationToken);
        return Ok(result);
    }
}
