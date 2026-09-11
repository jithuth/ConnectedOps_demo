using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Vehicles;

[ApiController]
[Route("api/vehicles/makes-models")]
[Authorize]
public sealed class VehicleMakesModelsController : ControllerBase
{
    private readonly IVehicleMakeModelService _makeModelService;

    public VehicleMakesModelsController(IVehicleMakeModelService makeModelService)
    {
        _makeModelService = makeModelService;
    }

    // Makes
    [HttpGet("makes")]
    [RequirePermission(PermissionKeys.VehicleMakes.View)]
    public async Task<IActionResult> GetMakes(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.GetMakesAsync(includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("makes/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleMakes.View)]
    public async Task<IActionResult> GetMakeById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.GetMakeByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("makes")]
    [RequirePermission(PermissionKeys.VehicleMakes.Manage)]
    public async Task<IActionResult> CreateMake(
        [FromBody] CreateVehicleMakeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.CreateMakeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetMakeById), new { id = result.Id }, result);
    }

    [HttpPut("makes/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleMakes.Manage)]
    public async Task<IActionResult> UpdateMake(
        Guid id,
        [FromBody] UpdateVehicleMakeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.UpdateMakeAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("makes/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleMakes.Manage)]
    public async Task<IActionResult> DeleteMake(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _makeModelService.DeleteMakeAsync(id, cancellationToken);
        return NoContent();
    }

    // Models
    [HttpGet("makes/{makeId:guid}/models")]
    [RequirePermission(PermissionKeys.VehicleModels.View)]
    public async Task<IActionResult> GetModelsByMake(
        Guid makeId,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.GetModelsByMakeIdAsync(makeId, includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("models")]
    [RequirePermission(PermissionKeys.VehicleModels.View)]
    public async Task<IActionResult> GetAllModels(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.GetAllModelsAsync(includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("models/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleModels.View)]
    public async Task<IActionResult> GetModelById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.GetModelByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("models")]
    [RequirePermission(PermissionKeys.VehicleModels.Manage)]
    public async Task<IActionResult> CreateModel(
        [FromBody] CreateVehicleModelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.CreateModelAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetModelById), new { id = result.Id }, result);
    }

    [HttpPut("models/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleModels.Manage)]
    public async Task<IActionResult> UpdateModel(
        Guid id,
        [FromBody] UpdateVehicleModelRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _makeModelService.UpdateModelAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("models/{id:guid}")]
    [RequirePermission(PermissionKeys.VehicleModels.Manage)]
    public async Task<IActionResult> DeleteModel(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _makeModelService.DeleteModelAsync(id, cancellationToken);
        return NoContent();
    }
}
