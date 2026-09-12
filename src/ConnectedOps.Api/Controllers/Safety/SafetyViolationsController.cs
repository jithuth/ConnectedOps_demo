using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Safety;

[ApiController]
[Route("api/safety/violations")]
[Authorize]
public sealed class SafetyViolationsController : ControllerBase
{
    private readonly ISafetyViolationService _violationService;

    public SafetyViolationsController(ISafetyViolationService violationService)
    {
        _violationService = violationService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.SafetyViolations.View)]
    public async Task<IActionResult> GetAll([FromQuery] SafetyViolationFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _violationService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.SafetyViolations.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _violationService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.SafetyViolations.Create)]
    public async Task<IActionResult> Create([FromBody] CreateSafetyViolationRequest request, CancellationToken cancellationToken)
    {
        var result = await _violationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.SafetyViolations.Edit)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSafetyViolationRequest request, CancellationToken cancellationToken)
    {
        var result = await _violationService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/resolve")]
    [RequirePermission(PermissionKeys.SafetyViolations.Resolve)]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveSafetyViolationRequest request, CancellationToken cancellationToken)
    {
        var result = await _violationService.ResolveAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.SafetyViolations.Edit)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _violationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
