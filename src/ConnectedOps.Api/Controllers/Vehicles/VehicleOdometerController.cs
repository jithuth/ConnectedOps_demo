using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Vehicles;

[ApiController]
[Route("api/vehicles/{vehicleId:guid}/odometer")]
[Authorize]
public sealed class VehicleOdometerController : ControllerBase
{
    private readonly IVehicleOdometerService _odometerService;

    public VehicleOdometerController(IVehicleOdometerService odometerService)
    {
        _odometerService = odometerService;
    }

    [HttpGet]
    [RequirePermission(PermissionKeys.Vehicles.View)]
    public async Task<IActionResult> GetHistory(
        Guid vehicleId,
        CancellationToken cancellationToken)
    {
        var result = await _odometerService.GetOdometerHistoryAsync(vehicleId, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [RequirePermission(PermissionKeys.Vehicles.ManageOdometer)]
    public async Task<IActionResult> RecordReading(
        Guid vehicleId,
        [FromBody] RecordVehicleOdometerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _odometerService.RecordOdometerAsync(vehicleId, request, cancellationToken);
        return Ok(result);
    }
}
