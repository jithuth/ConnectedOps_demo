using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/drivers/{driverId:guid}/licenses")]
[Authorize]
public sealed class DriverLicensesController : ControllerBase
{
    private readonly IDriverLicenseService _licenseService;

    public DriverLicensesController(IDriverLicenseService licenseService)
    {
        _licenseService = licenseService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.DriverLicenses.View)]
    public async Task<IActionResult> GetLicenses(
        Guid driverId,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.GetLicensesAsync(driverId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{licenseId:guid}")]
    [RequirePermission(PermissionKeys.DriverLicenses.View)]
    public async Task<IActionResult> GetById(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.GetLicenseByIdAsync(driverId, licenseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> AddLicense(
        Guid driverId,
        [FromBody] CreateDriverLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.AddLicenseAsync(driverId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { driverId, licenseId = result.Id }, result);
    }

    [HttpPut("{licenseId:guid}")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> UpdateLicense(
        Guid driverId,
        Guid licenseId,
        [FromBody] UpdateDriverLicenseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.UpdateLicenseAsync(driverId, licenseId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{licenseId:guid}/set-primary")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> SetPrimary(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.SetPrimaryLicenseAsync(driverId, licenseId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{licenseId:guid}/deactivate")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> Deactivate(
        Guid driverId,
        Guid licenseId,
        CancellationToken cancellationToken)
    {
        await _licenseService.DeactivateLicenseAsync(driverId, licenseId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{licenseId:guid}/categories")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> AddCategory(
        Guid driverId,
        Guid licenseId,
        [FromBody] AddDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.AddCategoryAsync(driverId, licenseId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{licenseId:guid}/categories/{categoryId:guid}")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> UpdateCategory(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        [FromBody] UpdateDriverLicenseCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _licenseService.UpdateCategoryAsync(driverId, licenseId, categoryId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{licenseId:guid}/categories/{categoryId:guid}")]
    [RequirePermission(PermissionKeys.DriverLicenses.Manage)]
    public async Task<IActionResult> DeleteCategory(
        Guid driverId,
        Guid licenseId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await _licenseService.DeleteCategoryAsync(driverId, licenseId, categoryId, cancellationToken);
        return NoContent();
    }
}
