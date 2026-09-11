using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Drivers;

[ApiController]
[Route("api/drivers")]
[Authorize]
public sealed class DriversController : ControllerBase
{
    private readonly IDriverService _driverService;

    public DriversController(IDriverService driverService)
    {
        _driverService = driverService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Drivers.View)]
    public async Task<IActionResult> GetDriversPaged(
        [FromQuery] DriverQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.GetDriversPagedAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Drivers.View)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.GetDriverByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Drivers.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.CreateDriverAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Drivers.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateDriverAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [RequirePermission(PermissionKeys.Drivers.ChangeStatus)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeDriverStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.ChangeStatusAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/activate")]
    [RequirePermission(PermissionKeys.Drivers.ChangeStatus)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.ChangeStatusAsync(id, new ChangeDriverStatusRequest(DriverStatus.Active, "Activated via API"), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [RequirePermission(PermissionKeys.Drivers.ChangeStatus)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.ChangeStatusAsync(id, new ChangeDriverStatusRequest(DriverStatus.Inactive, "Deactivated via API"), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/employee-link")]
    [RequirePermission(PermissionKeys.Drivers.LinkEmployee)]
    public async Task<IActionResult> LinkEmployee(
        Guid id,
        [FromBody] LinkDriverEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.LinkEmployeeAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/branch")]
    [RequirePermission(PermissionKeys.Drivers.ManageOrganization)]
    public async Task<IActionResult> AssignBranch(
        Guid id,
        [FromBody] AssignDriverBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.AssignBranchAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/department")]
    [RequirePermission(PermissionKeys.Drivers.ManageOrganization)]
    public async Task<IActionResult> AssignDepartment(
        Guid id,
        [FromBody] AssignDriverDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.AssignDepartmentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/notes")]
    [RequirePermission(PermissionKeys.Drivers.Edit)]
    public async Task<IActionResult> AddNote(
        Guid id,
        [FromBody] CreateDriverNoteRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.AddNoteAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/emergency-contacts")]
    [RequirePermission(PermissionKeys.Drivers.Edit)]
    public async Task<IActionResult> AddEmergencyContact(
        Guid id,
        [FromBody] CreateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.AddEmergencyContactAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission(PermissionKeys.Drivers.Edit)]
    public async Task<IActionResult> UpdateEmergencyContact(
        Guid id,
        Guid contactId,
        [FromBody] UpdateDriverEmergencyContactRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateEmergencyContactAsync(id, contactId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}/emergency-contacts/{contactId:guid}")]
    [RequirePermission(PermissionKeys.Drivers.Edit)]
    public async Task<IActionResult> DeleteEmergencyContact(
        Guid id,
        Guid contactId,
        CancellationToken cancellationToken)
    {
        await _driverService.DeleteEmergencyContactAsync(id, contactId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Drivers.Delete)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _driverService.DeleteDriverAsync(id, cancellationToken);
        return NoContent();
    }
}
