using ConnectedOps.Application.Maintenance;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Maintenance;

[ApiController]
[Route("api/maintenance/service-types")]
[Authorize]
public sealed class MaintenanceServiceTypesController : ControllerBase
{
    private readonly IMaintenanceServiceTypeService _serviceTypeService;

    public MaintenanceServiceTypesController(IMaintenanceServiceTypeService serviceTypeService)
    {
        _serviceTypeService = serviceTypeService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.MaintenanceServiceTypes.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _serviceTypeService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceServiceTypes.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _serviceTypeService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.MaintenanceServiceTypes.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _serviceTypeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceServiceTypes.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _serviceTypeService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.MaintenanceServiceTypes.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _serviceTypeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
