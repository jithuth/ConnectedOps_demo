using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/settings")]
[Authorize]
public sealed class OrganizationSettingsController : ControllerBase
{
    private readonly IOrganizationSettingsService _settingsService;

    public OrganizationSettingsController(IOrganizationSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Organization.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [RequirePermission(PermissionKeys.Organization.Manage)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateOrganizationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.UpdateSettingsAsync(request, cancellationToken);
        return Ok(result);
    }
}
