using ConnectedOps.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// keep your ORIGINAL project-specific usings here

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserService _currentUserService;

    public AuthController(
        IAuthenticationService authenticationService,
        ICurrentUserService currentUserService)
    {
        _authenticationService = authenticationService;
        _currentUserService = currentUserService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("select-tenant")]
    [AllowAnonymous]
    public async Task<IActionResult> SelectTenant(
        [FromBody] SelectTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.SelectTenantAsync(
            request,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _authenticationService.RefreshAsync(
            request,
            ipAddress,
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?.ToString();

        await _authenticationService.LogoutAsync(
            request,
            ipAddress,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        var ipAddress =
            HttpContext.Connection.RemoteIpAddress?.ToString();

        await _authenticationService.LogoutAllAsync(
            userId,
            ipAddress,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(
        CancellationToken cancellationToken)
    {
        var result =
            await _currentUserService.GetCurrentUserAsync(
                cancellationToken);

        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue("user_id")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new UnauthorizedAccessException(
                "Authenticated user ID is missing or invalid.");
        }

        return userId;
    }
}