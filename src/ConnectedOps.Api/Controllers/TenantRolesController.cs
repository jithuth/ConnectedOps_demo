using ConnectedOps.Application.Permissions;
using ConnectedOps.Application.TenantRoles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// KEEP your existing using statement that contains RequirePermission.
// Do NOT add:
// using ConnectedOps.Application.Authorization;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/tenant-roles")]
[Authorize]
public sealed class TenantRolesController : ControllerBase
{
    private readonly ITenantRoleManagementService _roleService;
    private readonly IPermissionManagementService _permissionService;

    public TenantRolesController(
        ITenantRoleManagementService roleService,
        IPermissionManagementService permissionService)
    {
        _roleService = roleService;
        _permissionService = permissionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetRoles(
        CancellationToken cancellationToken)
    {
        var result =
            await _roleService.GetRolesAsync(
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{roleId:guid}")]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result =
            await _roleService.GetRoleAsync(
                roleId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> CreateRole(
        [FromBody] CreateTenantRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _roleService.CreateAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("{roleId:guid}")]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> UpdateRole(
        Guid roleId,
        [FromBody] UpdateTenantRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _roleService.UpdateAsync(
            roleId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{roleId:guid}")]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> DeleteRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        await _roleService.DeleteAsync(
            roleId,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{roleId:guid}/permissions")]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> GetRolePermissions(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result =
            await _permissionService.GetRolePermissionsAsync(
                roleId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("{roleId:guid}/permissions")]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> UpdateRolePermissions(
        Guid roleId,
        [FromBody] UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        await _permissionService.UpdateRolePermissionsAsync(
            roleId,
            request,
            cancellationToken);

        return NoContent();
    }
}