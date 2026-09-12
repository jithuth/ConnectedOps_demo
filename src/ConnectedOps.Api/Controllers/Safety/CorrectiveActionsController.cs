using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Safety;

[ApiController]
[Route("api/safety/corrective-actions")]
[Authorize]
public sealed class CorrectiveActionsController : ControllerBase
{
    private readonly ICorrectiveActionService _actionService;

    public CorrectiveActionsController(ICorrectiveActionService actionService)
    {
        _actionService = actionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.CorrectiveActions.View)]
    public async Task<IActionResult> GetAll([FromQuery] CorrectiveActionFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _actionService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.CorrectiveActions.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _actionService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Start(Guid id, CancellationToken cancellationToken)
    {
        var result = await _actionService.StartAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionService.CompleteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/verify")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Verify)]
    public async Task<IActionResult> Verify(Guid id, [FromBody] VerifyCorrectiveActionRequest request, CancellationToken cancellationToken)
    {
        var result = await _actionService.VerifyAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _actionService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.CorrectiveActions.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _actionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
