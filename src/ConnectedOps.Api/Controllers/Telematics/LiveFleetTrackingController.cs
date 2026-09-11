using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Telematics;

[ApiController]
[Route("api/telematics/live")]
[Authorize]
public sealed class LiveFleetTrackingController : ControllerBase
{
    private readonly IVehicleTelemetryStateService _stateService;

    public LiveFleetTrackingController(IVehicleTelemetryStateService stateService)
    {
        _stateService = stateService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Telematics.ViewLive)]
    public async Task<IActionResult> GetLiveFleet(
        [FromQuery] LiveTrackingQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _stateService.GetLiveFleetTrackingAsync(parameters, cancellationToken);
        return Ok(result);
    }
}
