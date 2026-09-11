using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Route("api/fuel/types")]
[Authorize]
public sealed class FuelTypesController : ControllerBase
{
    private readonly IFuelTypeService _fuelTypeService;

    public FuelTypesController(IFuelTypeService fuelTypeService)
    {
        _fuelTypeService = fuelTypeService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Fuel.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _fuelTypeService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Fuel.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _fuelTypeService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Fuel.View)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _fuelTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Fuel.View)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFuelTypeDefinitionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _fuelTypeService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Fuel.View)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _fuelTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
