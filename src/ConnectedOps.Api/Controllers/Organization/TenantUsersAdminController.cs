using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/users")]
[Authorize]
public sealed class TenantUsersAdminController : ControllerBase
{
    private readonly ITenantUserAdministrationService _userService;

    public TenantUsersAdminController(ITenantUserAdministrationService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Users.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Users.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTenantUserAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.TenantUserId }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Users.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTenantUserAdminRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.Users.Edit)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _userService.ActivateUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionKeys.Users.Edit)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _userService.DeactivateUserAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Users.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _userService.RemoveUserAsync(id, cancellationToken);
        return NoContent();
    }
}
