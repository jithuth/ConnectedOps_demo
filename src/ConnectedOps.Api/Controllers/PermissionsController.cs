using ConnectedOps.Application.Permissions;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// KEEP your existing using for RequirePermission.
// DO NOT use ConnectedOps.Application.Authorization.

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/permissions")]
[Authorize]
public sealed class PermissionsController : ControllerBase
{
    private readonly IPermissionManagementService _permissionService;

    public PermissionsController(
        IPermissionManagementService permissionService)
    {
        _permissionService = permissionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> GetPermissions(
        CancellationToken cancellationToken)
    {
        var result =
            await _permissionService.GetPermissionsAsync(
                cancellationToken);

        return Ok(result);
    }
}