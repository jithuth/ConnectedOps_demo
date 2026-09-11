using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/profile")]
[Authorize]
public sealed class OrganizationProfileController : ControllerBase
{
    private readonly IOrganizationProfileService _profileService;

    public OrganizationProfileController(IOrganizationProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Organization.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _profileService.GetProfileAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [RequirePermission(PermissionKeys.Organization.Manage)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _profileService.UpdateProfileAsync(request, cancellationToken);
        return Ok(result);
    }
}
