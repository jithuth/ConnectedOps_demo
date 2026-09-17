using ConnectedOps.Application.TollsAndFines;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.TollsAndFines;

[ApiController]
[Route("api/tolls-and-fines")]
[Authorize]
public sealed class TollsAndFinesController : ControllerBase
{
    private readonly ITollAndFineService _tollAndFineService;

    public TollsAndFinesController(ITollAndFineService tollAndFineService)
    {
        _tollAndFineService = tollAndFineService;
    }

    [HttpGet("dashboard")]
    [RequirePermission(PermissionKeys.TollsAndFines.View)]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("tolls")]
    [RequirePermission(PermissionKeys.TollsAndFines.View)]
    public async Task<IActionResult> GetTolls(
        [FromQuery] TollFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.GetTollsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost("tolls")]
    [RequirePermission(PermissionKeys.TollsAndFines.Create)]
    public async Task<IActionResult> CreateToll(
        [FromBody] CreateTollTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.CreateTollAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("violations")]
    [RequirePermission(PermissionKeys.TollsAndFines.View)]
    public async Task<IActionResult> GetViolations(
        [FromQuery] ViolationFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.GetViolationsPagedAsync(filter, cancellationToken);
        return Ok(result);
    }

    [HttpPost("violations")]
    [RequirePermission(PermissionKeys.TollsAndFines.Create)]
    public async Task<IActionResult> CreateViolation(
        [FromBody] CreateTrafficViolationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.CreateViolationAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("violations/{id:guid}/assign")]
    [RequirePermission(PermissionKeys.TollsAndFines.AssignDriver)]
    public async Task<IActionResult> AssignLiability(
        Guid id,
        [FromBody] AssignViolationLiabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.AssignLiabilityAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("violations/{id:guid}/dispute")]
    [RequirePermission(PermissionKeys.TollsAndFines.ResolveDispute)]
    public async Task<IActionResult> DisputeViolation(
        Guid id,
        [FromBody] DisputeViolationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.DisputeViolationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("violations/{id:guid}/settle")]
    [RequirePermission(PermissionKeys.TollsAndFines.ResolveDispute)]
    public async Task<IActionResult> SettleViolation(
        Guid id,
        [FromBody] SettleViolationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tollAndFineService.SettleViolationAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("automatch")]
    [RequirePermission(PermissionKeys.TollsAndFines.AssignDriver)]
    public async Task<IActionResult> AutoMatchDriverLiability(CancellationToken cancellationToken)
    {
        var count = await _tollAndFineService.AutoMatchDriverLiabilityAsync(cancellationToken);
        return Ok(new { matchedCount = count });
    }
}
