using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/dashboard")]
[Authorize]
public sealed class OrganizationDashboardController : ControllerBase
{
    private readonly IOrganizationDashboardService _dashboardService;

    public OrganizationDashboardController(IOrganizationDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.OrganizationDashboard.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
