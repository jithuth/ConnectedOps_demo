using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/departments")]
[Authorize]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;

    public DepartmentsController(IDepartmentService departmentService)
    {
        _departmentService = departmentService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Departments.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetDepartmentsAsync(branchId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Departments.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _departmentService.GetDepartmentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Departments.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.CreateDepartmentAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Departments.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _departmentService.UpdateDepartmentAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Departments.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _departmentService.DeleteDepartmentAsync(id, cancellationToken);
        return NoContent();
    }
}
