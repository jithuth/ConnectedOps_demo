using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/locations")]
[Authorize]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Locations.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.GetLocationsAsync(branchId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Locations.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _locationService.GetLocationByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Locations.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.CreateLocationAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Locations.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _locationService.UpdateLocationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Locations.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _locationService.DeleteLocationAsync(id, cancellationToken);
        return NoContent();
    }
}
