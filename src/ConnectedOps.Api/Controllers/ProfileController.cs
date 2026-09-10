using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController
    : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        var userId =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        var tenantId =
            User.FindFirstValue(
                "tenant_id");

        var tenantUserId =
            User.FindFirstValue(
                "tenant_user_id");

        return Ok(new
        {
            userId,
            tenantId,
            tenantUserId
        });
    }
}