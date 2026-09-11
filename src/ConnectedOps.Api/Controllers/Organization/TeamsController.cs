using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Organization;

[ApiController]
[Route("api/organization/teams")]
[Authorize]
public sealed class TeamsController : ControllerBase
{
    private readonly ITeamService _teamService;

    public TeamsController(ITeamService teamService)
    {
        _teamService = teamService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Teams.View)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? departmentId,
        CancellationToken cancellationToken)
    {
        var result = await _teamService.GetTeamsAsync(departmentId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Teams.View)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _teamService.GetTeamByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Teams.Manage)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _teamService.CreateTeamAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionKeys.Teams.Manage)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _teamService.UpdateTeamAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionKeys.Teams.Manage)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _teamService.DeleteTeamAsync(id, cancellationToken);
        return NoContent();
    }
}
