using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Vehicles;

[ApiController]
[Route("api/vehicles")]
[Authorize]
public sealed class VehiclesController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehiclesController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Vehicles.View)]
    public async Task<IActionResult> GetVehiclesPaged(
        [FromQuery] VehicleQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetVehiclesPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.GetVehicleByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Vehicles.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.CreateVehicleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateVehicleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.UpdateVehicleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/status")]
    [RequirePermission(PermissionKeys.Vehicles.ManageStatus)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeVehicleStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/branch")]
    [RequirePermission(PermissionKeys.Vehicles.Edit)]
    public async Task<IActionResult> AssignBranch(
        Guid id,
        [FromBody] AssignVehicleBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.AssignBranchAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/location")]
    [RequirePermission(PermissionKeys.Vehicles.Edit)]
    public async Task<IActionResult> AssignLocation(
        Guid id,
        [FromBody] AssignVehicleLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.AssignLocationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/specification")]
    [RequirePermission(PermissionKeys.Vehicles.Edit)]
    public async Task<IActionResult> UpsertSpecification(
        Guid id,
        [FromBody] UpsertVehicleSpecificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.UpsertSpecificationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/registrations")]
    [RequirePermission(PermissionKeys.Vehicles.Edit)]
    public async Task<IActionResult> AddRegistration(
        Guid id,
        [FromBody] CreateVehicleRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.AddRegistrationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/notes")]
    [RequirePermission(PermissionKeys.Vehicles.ManageNotes)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] CreateVehicleNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vehicleService.AddNoteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Vehicles.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _vehicleService.DeleteVehicleAsync(id, cancellationToken);
        return NoContent();
    }
}
