using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/asset-types")]
[Authorize]
public sealed class AssetTypesController : ControllerBase
{
    private readonly IAssetTypeService _typeService;

    public AssetTypesController(IAssetTypeService typeService)
    {
        _typeService = typeService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.AssetTypes.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var result = await _typeService.GetAllAsync(categoryId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetTypes.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _typeService.GetByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.AssetTypes.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _typeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetTypes.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAssetTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _typeService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetTypes.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _typeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
