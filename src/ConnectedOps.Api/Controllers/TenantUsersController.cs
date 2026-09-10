using ConnectedOps.Application.TenantUsers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/tenant-users")]
[Authorize]
public sealed class TenantUsersController : ControllerBase
{
    private readonly ITenantUserManagementService _tenantUserService;

    public TenantUsersController(
        ITenantUserManagementService tenantUserService)
    {
        _tenantUserService = tenantUserService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetUsers(
        CancellationToken cancellationToken)
    {
        var result =
            await _tenantUserService.GetUsersAsync(
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{tenantUserId:guid}")]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetUser(
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        var result =
            await _tenantUserService.GetUserAsync(
                tenantUserId,
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("{tenantUserId:guid}/roles")]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> UpdateRoles(
        Guid tenantUserId,
        [FromBody] UpdateTenantUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        await _tenantUserService.UpdateRolesAsync(
            tenantUserId,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{tenantUserId:guid}/activate")]
    [RequirePermission(PermissionKeys.Users.Edit)]
    public async Task<IActionResult> Activate(
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        await _tenantUserService.ActivateAsync(
            tenantUserId,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{tenantUserId:guid}/deactivate")]
    [RequirePermission(PermissionKeys.Users.Edit)]
    public async Task<IActionResult> Deactivate(
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        await _tenantUserService.DeactivateAsync(
            tenantUserId,
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{tenantUserId:guid}")]
    [RequirePermission(PermissionKeys.Users.Delete)]
    public async Task<IActionResult> Remove(
        Guid tenantUserId,
        CancellationToken cancellationToken)
    {
        await _tenantUserService.RemoveAsync(
            tenantUserId,
            cancellationToken);

        return NoContent();
    }
}