using ConnectedOps.Application.Assets;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Assets;

[ApiController]
[Route("api/asset-categories")]
[Authorize]
public sealed class AssetCategoriesController : ControllerBase
{
    private readonly IAssetCategoryService _categoryService;

    public AssetCategoriesController(IAssetCategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.AssetCategories.View)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetAllAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("tree")]
    [RequirePermission(PermissionKeys.AssetCategories.View)]
    public async Task<IActionResult> GetTree(CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetTreeAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetCategories.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetByIdAsync(id, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.AssetCategories.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetCategories.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAssetCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.AssetCategories.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _categoryService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
