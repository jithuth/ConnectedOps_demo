using ConnectedOps.Application.Demo;
using ConnectedOps.Application.Maps;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace ConnectedOps.Web.Pages.Demo;

[Authorize]
public sealed class FleetSimulatorModel : PageModel
{
    private readonly IDemoFleetSimulator _simulator;
    private readonly IMasterEnterpriseDemoSeeder _masterDemoSeeder;
    private readonly DemoFleetSettings _settings;

    public FleetSimulatorModel(
        IDemoFleetSimulator simulator,
        IMasterEnterpriseDemoSeeder masterDemoSeeder,
        IOptions<DemoFleetSettings> settings)
    {
        _simulator = simulator;
        _masterDemoSeeder = masterDemoSeeder;
        _settings = settings.Value;
    }

    public bool IsFeatureEnabled => _settings.Enabled;
    public DemoFleetStatusDto Status { get; private set; } = new(false, false, 0, 0, 3, null, 0, "Uninitialized");

    public async Task OnGetAsync()
    {
        var ct = HttpContext.RequestAborted;
        Status = await _simulator.GetStatusAsync(ct);
    }

    public async Task<IActionResult> OnGetStatusAsync()
    {
        var ct = HttpContext.RequestAborted;
        var status = await _simulator.GetStatusAsync(ct);
        var vehicles = await _simulator.GetDemoVehiclesAsync(ct);
        return new JsonResult(new
        {
            status = status,
            vehicles = vehicles
        });
    }

    public async Task<IActionResult> OnPostCreateFleetAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var status = await _simulator.CreateDemoFleetAsync(new CreateDemoFleetRequest(), ct);
            return new JsonResult(new { success = true, status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostStartAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var status = await _simulator.StartSimulationAsync(ct);
            return new JsonResult(new { success = true, status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostStopAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var status = await _simulator.StopSimulationAsync(ct);
            return new JsonResult(new { success = true, status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostStepAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var status = await _simulator.StepSimulationAsync(ct);
            return new JsonResult(new { success = true, status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var status = await _simulator.ResetSimulationAsync(ct);
            return new JsonResult(new { success = true, status });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostToggleOfflineAsync(Guid vehicleId)
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var result = await _simulator.ToggleVehicleOfflineSimulationAsync(vehicleId, ct);
            return new JsonResult(new { success = true, isSimulatingOffline = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    public async Task<IActionResult> OnPostSeedEnterpriseDemoAsync()
    {
        var ct = HttpContext.RequestAborted;
        try
        {
            var result = await _masterDemoSeeder.SeedEnterpriseDemoDataAsync(null, ct);
            return new JsonResult(new { success = true, result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}
