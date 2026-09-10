using ConnectedOps.Application.Platform;
using ConnectedOps.Domain.Platform;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Platform;

[ApiController]
[Route("api/platform/settings")]
[Authorize]
[RequirePlatformRole(PlatformRole.SuperAdmin)]
public sealed class PlatformSettingsController : ControllerBase
{
    private readonly IPlatformSettingsService _settingsService;

    public PlatformSettingsController(IPlatformSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<PlatformSettingsDto>> GetSettings(
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.GetSettingsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<PlatformSettingsDto>> UpdateSettings(
        [FromBody] UpdatePlatformSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _settingsService.UpdateSettingsAsync(request, cancellationToken);
        return Ok(result);
    }
}
