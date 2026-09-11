using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/hierarchy")]
[Authorize]
public sealed class OrganizationHierarchyController : ControllerBase
{
    private readonly IOrganizationHierarchyService _hierarchyService;

    public OrganizationHierarchyController(IOrganizationHierarchyService hierarchyService)
    {
        _hierarchyService = hierarchyService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Organization.View)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _hierarchyService.GetHierarchyAsync(cancellationToken);
        return Ok(result);
    }
}
