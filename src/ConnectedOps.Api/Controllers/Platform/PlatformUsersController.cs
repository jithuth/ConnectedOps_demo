using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/users")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformUsersController : ControllerBase
{
    private readonly IPlatformUserService _userService;

    public PlatformUsersController(IPlatformUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformUserPage>> GetUsers(
        [FromQuery] PlatformUserQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUsersAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<PlatformUserDetailDto>> GetUserById(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CreatePlatformUserResult>> CreateUser(
        [FromBody] CreatePlatformUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.CreateUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUserById), new { userId = result.User.Id }, result);
    }

    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid userId,
        [FromBody] UpdatePlatformUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.UpdateUserRoleAsync(userId, request.Role, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/activate")]
    public async Task<IActionResult> ActivateUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _userService.ActivateUserAsync(userId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _userService.DeactivateUserAsync(userId, cancellationToken);
        return NoContent();
    }
}
