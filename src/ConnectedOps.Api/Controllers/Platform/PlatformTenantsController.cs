using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/tenants")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformTenantsController : ControllerBase
{
    private readonly IPlatformTenantService _tenantService;

    public PlatformTenantsController(IPlatformTenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformTenantPage>> GetTenants(
        [FromQuery] PlatformTenantQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}")]
    public async Task<ActionResult<PlatformTenantDetailDto>> GetTenantById(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantByIdAsync(tenantId, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PlatformTenantDto>> CreateTenant(
        [FromBody] CreatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.CreateTenantAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTenantById), new { tenantId = result.Id }, result);
    }

    [HttpPut("{tenantId:guid}")]
    public async Task<ActionResult<PlatformTenantDto>> UpdateTenant(
        Guid tenantId,
        [FromBody] UpdatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.UpdateTenantAsync(tenantId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{tenantId:guid}/activate")]
    public async Task<IActionResult> ActivateTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await _tenantService.ActivateTenantAsync(tenantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/suspend")]
    public async Task<IActionResult> SuspendTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await _tenantService.SuspendTenantAsync(tenantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{tenantId:guid}/disable")]
    public async Task<IActionResult> DisableTenant(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        await _tenantService.DisableTenantAsync(tenantId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{tenantId:guid}/users")]
    public async Task<ActionResult<IReadOnlyCollection<PlatformTenantUserDto>>> GetTenantUsers(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantUsersAsync(tenantId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}/statistics")]
    public async Task<ActionResult<PlatformTenantStatsDto>> GetTenantStatistics(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetTenantStatisticsAsync(tenantId, cancellationToken);
        return Ok(result);
    }
}
