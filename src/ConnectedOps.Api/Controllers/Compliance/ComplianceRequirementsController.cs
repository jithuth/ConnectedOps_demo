using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Compliance;

[ApiController]
[Route("api/compliance/requirements")]
[Authorize]
public sealed class ComplianceRequirementsController : ControllerBase
{
    private readonly IComplianceRequirementService _requirementService;

    public ComplianceRequirementsController(IComplianceRequirementService requirementService)
    {
        _requirementService = requirementService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.ComplianceRequirements.View)]
    public async Task<IActionResult> GetAll([FromQuery] ComplianceRequirementFilterRequest filter, CancellationToken cancellationToken)
    {
        var result = await _requirementService.GetPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("active")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.View)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await _requirementService.GetAllActiveAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _requirementService.GetByIdAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> Create([FromBody] CreateComplianceRequirementRequest request, CancellationToken cancellationToken)
    {
        var result = await _requirementService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateComplianceRequirementRequest request, CancellationToken cancellationToken)
    {
        var result = await _requirementService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _requirementService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/rules")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> AddRule(Guid id, [FromBody] CreateComplianceRequirementRuleRequest request, CancellationToken cancellationToken)
    {
        var req = request with { ComplianceRequirementId = id };
        var result = await _requirementService.AddRuleAsync(req, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{requirementId:guid}/rules/{ruleId:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> UpdateRule(Guid requirementId, Guid ruleId, [FromBody] UpdateComplianceRequirementRuleRequest request, CancellationToken cancellationToken)
    {
        var result = await _requirementService.UpdateRuleAsync(requirementId, ruleId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{requirementId:guid}/rules/{ruleId:guid}")]
    [RequirePermission(PermissionKeys.ComplianceRequirements.Manage)]
    public async Task<IActionResult> DeleteRule(Guid requirementId, Guid ruleId, CancellationToken cancellationToken)
    {
        await _requirementService.DeleteRuleAsync(requirementId, ruleId, cancellationToken);
        return NoContent();
    }
}
