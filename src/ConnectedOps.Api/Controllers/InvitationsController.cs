
using ConnectedOps.Application.Invitations;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/invitations")]
public sealed class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(
        IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost]
    [Authorize]
    [RequirePermission(PermissionKeys.Users.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _invitationService.CreateAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [HttpPost("accept")]
    [AllowAnonymous]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _invitationService.AcceptAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{invitationId:guid}")]
    [Authorize]
    [RequirePermission(PermissionKeys.Users.ManageRoles)]
    public async Task<IActionResult> Revoke(
        Guid invitationId,
        CancellationToken cancellationToken)
    {
        await _invitationService.RevokeAsync(
            invitationId,
            cancellationToken);

        return NoContent();
    }
}