using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/telematics/settings")]
[Authorize]
public sealed class TelematicsSettingsController : ControllerBase
{
    private readonly ITelematicsSettingsService _settingsService;

    public TelematicsSettingsController(ITelematicsSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Telematics.ViewDashboard)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut]
    [RequirePermission(PermissionKeys.Telematics.ViewDashboard)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateTelematicsSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.UpdateSettingsAsync(request, cancellationToken);
        return Ok(settings);
    }
}
