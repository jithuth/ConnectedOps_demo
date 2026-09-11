using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/plans")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionPlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SubscriptionPlanDto>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var result = await _planService.GetAllPlansAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanDto>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _planService.GetPlanByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionPlanDto>> Create(
        [FromBody] CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _planService.CreatePlanAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionPlanDto>> Update(
        Guid id,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _planService.UpdatePlanAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _planService.ActivatePlanAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _planService.DeactivatePlanAsync(id, cancellationToken);
        return NoContent();
    }
}
