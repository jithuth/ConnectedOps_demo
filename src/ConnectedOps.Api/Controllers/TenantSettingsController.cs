using ConnectedOps.Application.TenantSettings;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// KEEP existing RequirePermission namespace.
// Do NOT add ConnectedOps.Application.Authorization.

namespace ConnectedOps.Api.Controllers;

[ApiController]
[Route("api/tenant-settings")]
[Authorize]
public sealed class TenantSettingsController : ControllerBase
{
    private readonly ITenantSettingsService _settingsService;

    public TenantSettingsController(
        ITenantSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Tenants.View)]
    public async Task<IActionResult> Get(
        CancellationToken cancellationToken)
    {
        var result =
            await _settingsService.GetAsync(
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("general")]
    [RequirePermission(PermissionKeys.Tenants.Manage)]
    public async Task<IActionResult> UpdateGeneral(
        [FromBody] UpdateTenantGeneralSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _settingsService.UpdateGeneralAsync(
                request,
                cancellationToken);

        return Ok(result);
    }

    [HttpPut("notifications")]
    [RequirePermission(PermissionKeys.Tenants.Manage)]
    public async Task<IActionResult> UpdateNotifications(
        [FromBody] UpdateTenantNotificationSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _settingsService.UpdateNotificationsAsync(
                request,
                cancellationToken);

        return Ok(result);
    }
}