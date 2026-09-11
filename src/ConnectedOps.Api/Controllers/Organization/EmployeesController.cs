using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/employees")]
[Authorize]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Employees.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? departmentId,
        [FromQuery] Guid? teamId,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetEmployeesAsync(branchId, departmentId, teamId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Employees.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetEmployeeByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Employees.Create)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.CreateEmployeeAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Employees.Edit)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _employeeService.UpdateEmployeeAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Employees.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _employeeService.DeleteEmployeeAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/link-user")]
    [RequirePermission(PermissionKeys.Employees.Edit)]
    public async Task<IActionResult> LinkUser(
        Guid id,
        [FromBody] LinkEmployeeUserRequest request,
        CancellationToken cancellationToken)
    {
        await _employeeService.LinkUserAsync(id, request.UserId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlink-user")]
    [RequirePermission(PermissionKeys.Employees.Edit)]
    public async Task<IActionResult> UnlinkUser(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _employeeService.UnlinkUserAsync(id, cancellationToken);
        return NoContent();
    }
}
