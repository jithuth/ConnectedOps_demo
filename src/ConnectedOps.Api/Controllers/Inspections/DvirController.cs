using ConnectedOps.Application.Inspections;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Inspections;

[ApiController]
[Route("api/dvir")]
[Authorize]
public sealed class DvirController : ControllerBase
{
    private readonly IDvirService _dvirService;

    public DvirController(IDvirService dvirService)
    {
        _dvirService = dvirService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.Dvir.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _dvirService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Dvir.View)]
    public async Task<IActionResult> GetInspections(
        [FromQuery] DvirFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _dvirService.GetInspectionsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionKeys.Dvir.View)]
    public async Task<IActionResult> GetInspectionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dvirService.GetInspectionByIdAsync(id, cancellationToken);
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Dvir.Create)]
    public async Task<IActionResult> CreateInspection(
        [FromBody] CreateDvirRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dvirService.CreateInspectionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetInspectionById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/certify")]
    [RequirePermission(PermissionKeys.Dvir.SignOff)]
    public async Task<IActionResult> Certify(
        Guid id,
        [FromBody] SignOffDvirRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _dvirService.CertifyByMechanicAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
