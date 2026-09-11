using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Fuel;

[ApiController]
[Route("api/fuel/stations")]
[Authorize]
public sealed class FuelStationsController : ControllerBase
{
    private readonly IFuelStationService _stationService;

    public FuelStationsController(IFuelStationService stationService)
    {
        _stationService = stationService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.FuelStations.View)]
    public async Task<IActionResult> GetStations(
        [FromQuery] FuelStationQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await _stationService.GetStationsPagedAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("all")]
    [RequirePermission(PermissionKeys.FuelStations.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _stationService.GetAllAsync(activeOnly, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelStations.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _stationService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.FuelStations.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFuelStationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelStations.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFuelStationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _stationService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.FuelStations.Manage)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _stationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
