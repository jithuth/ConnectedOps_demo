using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Billing;

[ApiController]
[Route("api/billing/dashboard")]
[Authorize]
public sealed class BillingDashboardController : ControllerBase
{
    private readonly IBillingDashboardService _dashboardService;

    public BillingDashboardController(IBillingDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Invoices.View)]
    public async Task<ActionResult<BillingDashboardSummaryDto>> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
