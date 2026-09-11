using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/branches")]
[Authorize]
public sealed class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Branches.View)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _branchService.GetBranchesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Branches.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _branchService.GetBranchByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Branches.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.CreateBranchAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Branches.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBranchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _branchService.UpdateBranchAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Branches.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _branchService.DeleteBranchAsync(id, cancellationToken);
        return NoContent();
    }
}
