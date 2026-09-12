using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Compliance;

[ApiController]
[Route("api/compliance/exceptions")]
[Authorize]
public sealed class ComplianceExceptionsController : ControllerBase
{
    private readonly IComplianceExceptionService _exceptionService;

    public ComplianceExceptionsController(IComplianceExceptionService exceptionService)
    {
        _exceptionService = exceptionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.ComplianceExceptions.View)]
    public async Task<IActionResult> GetAll([FromQuery] ComplianceExceptionFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceExceptions.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.ComplianceExceptions.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateComplianceExceptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/approve")]
    [RequirePermission(PermissionKeys.ComplianceExceptions.Manage)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveComplianceExceptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.ApproveAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/reject")]
    [RequirePermission(PermissionKeys.ComplianceExceptions.Manage)]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectComplianceExceptionRequest request, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.RejectAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [RequirePermission(PermissionKeys.ComplianceExceptions.Manage)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _exceptionService.CancelAsync(id, cancellationToken);
        return Ok(result);
    }
}
