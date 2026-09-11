using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maintenance;

[ApiController]
[Route("api/maintenance/providers")]
[Authorize]
public sealed class MaintenanceProvidersController : ControllerBase
{
    private readonly IMaintenanceProviderService _providerService;

    public MaintenanceProvidersController(IMaintenanceProviderService providerService)
    {
        _providerService = providerService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.MaintenanceProviders.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _providerService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceProviders.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _providerService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.MaintenanceProviders.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceProviderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _providerService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceProviders.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMaintenanceProviderRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _providerService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceProviders.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _providerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
