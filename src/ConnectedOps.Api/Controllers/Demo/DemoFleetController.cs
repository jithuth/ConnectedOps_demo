using ConnectedOps.Application.Demo;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Demo;

[ApiController]
[Route("api/demo-fleet")]
[Authorize]
public sealed class DemoFleetController : ControllerBase
{
    private readonly IDemoFleetSimulator _simulator;

    public DemoFleetController(IDemoFleetSimulator simulator)
    {
        _simulator = simulator;
    }

    [HttpGet("status")]
    [RequirePermission(PermissionKeys.DemoFleet.View)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _simulator.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("create")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> CreateFleet(
        [FromBody] CreateDemoFleetRequest request,
        CancellationToken cancellationToken)
    {
        var status = await _simulator.CreateDemoFleetAsync(request, cancellationToken);
        return Ok(status);
    }

    [HttpPost("start")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> StartSimulation(CancellationToken cancellationToken)
    {
        var status = await _simulator.StartSimulationAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("stop")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> StopSimulation(CancellationToken cancellationToken)
    {
        var status = await _simulator.StopSimulationAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("reset")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> ResetSimulation(CancellationToken cancellationToken)
    {
        var status = await _simulator.ResetSimulationAsync(cancellationToken);
        return Ok(status);
    }

    [HttpPost("step")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> StepSimulation(CancellationToken cancellationToken)
    {
        var status = await _simulator.StepSimulationAsync(cancellationToken);
        return Ok(status);
    }

    [HttpGet("vehicles")]
    [RequirePermission(PermissionKeys.DemoFleet.View)]
    public async Task<IActionResult> GetDemoVehicles(CancellationToken cancellationToken)
    {
        var vehicles = await _simulator.GetDemoVehiclesAsync(cancellationToken);
        return Ok(vehicles);
    }

    [HttpPost("vehicles/{vehicleId:guid}/toggle-offline")]
    [RequirePermission(PermissionKeys.DemoFleet.Manage)]
    public async Task<IActionResult> ToggleVehicleOffline(Guid vehicleId, CancellationToken cancellationToken)
    {
        var success = await _simulator.ToggleVehicleOfflineSimulationAsync(vehicleId, cancellationToken);
        if (!success)
            return NotFound(new { error = $"Demo vehicle '{vehicleId}' was not found." });

        return Ok(new { success = true, vehicleId });
    }
}
