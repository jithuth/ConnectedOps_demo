using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Compliance;

[ApiController]
[Route("api/compliance")]
[Authorize]
public sealed class ComplianceSubjectsController : ControllerBase
{
    private readonly IComplianceEvaluationService _evaluationService;

    public ComplianceSubjectsController(IComplianceEvaluationService evaluationService)
    {
        _evaluationService = evaluationService;
    }

    [HttpGet("vehicles/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.View)]
    public async Task<IActionResult> EvaluateVehicle(Guid id, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.EvaluateVehicleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("drivers/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.View)]
    public async Task<IActionResult> EvaluateDriver(Guid id, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.EvaluateDriverAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("assets/{id:guid}")]
    [RequirePermission(PermissionKeys.Compliance.View)]
    public async Task<IActionResult> EvaluateAsset(Guid id, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.EvaluateAssetAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("directory")]
    [RequirePermission(PermissionKeys.Compliance.View)]
    public async Task<IActionResult> GetDirectory(
        [FromQuery] ComplianceSubjectType? subjectType = null,
        [FromQuery] Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _evaluationService.EvaluateAllSubjectsAsync(subjectType, branchId, cancellationToken);
        return Ok(result);
    }
}
