using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/assets/inspections")]
[Authorize]
public sealed class AssetInspectionsController : ControllerBase
{
    private readonly IAssetInspectionService _inspectionService;
    private readonly IAssetConditionService _conditionService;

    public AssetInspectionsController(
        IAssetInspectionService inspectionService,
        IAssetConditionService conditionService)
    {
        _inspectionService = inspectionService;
        _conditionService = conditionService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.AssetInspections.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] AssetInspectionFilter filter,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.GetInspectionsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetInspections.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.GetInspectionByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.AssetInspections.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.CreateInspectionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/complete")]
    [RequirePermission(PermissionKeys.AssetInspections.Manage)]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteAssetInspectionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.CompleteInspectionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    // Calibrations
    [HttpGet("asset/{assetId:guid}/calibrations")]
    [RequirePermission(PermissionKeys.AssetInspections.View)]
    public async Task<IActionResult> GetCalibrations(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.GetCalibrationsByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("calibrations")]
    [RequirePermission(PermissionKeys.AssetInspections.Manage)]
    public async Task<IActionResult> AddCalibration(
        [FromBody] CreateAssetCalibrationRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _inspectionService.AddCalibrationRecordAsync(request, cancellationToken);
        return Ok(result);
    }

    // Conditions
    [HttpGet("asset/{assetId:guid}/conditions")]
    [RequirePermission(PermissionKeys.AssetConditions.View)]
    public async Task<IActionResult> GetConditionHistory(
        Guid assetId,
        CancellationToken cancellationToken)
    {
        var result = await _conditionService.GetConditionHistoryByAssetAsync(assetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("conditions")]
    [RequirePermission(PermissionKeys.AssetConditions.Manage)]
    public async Task<IActionResult> RecordCondition(
        [FromBody] CreateAssetConditionRecordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _conditionService.RecordConditionAsync(request, cancellationToken);
        return Ok(result);
    }
}
