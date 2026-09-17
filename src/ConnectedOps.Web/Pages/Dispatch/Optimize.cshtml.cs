using ConnectedOps.Application.Optimization;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Optimization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Dispatch;

[Authorize]
public class OptimizeModel : PageModel
{
    private readonly IRouteOptimizationService _optimizationService;

    public OptimizeModel(IRouteOptimizationService optimizationService)
    {
        _optimizationService = optimizationService;
    }

    public PagedResult<RouteOptimizationRunDto> Runs { get; private set; } = null!;
    public RouteOptimizationRunDto? SelectedRun { get; private set; }

    [BindProperty(SupportsGet = true)]
    public Guid? RunId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Runs = await _optimizationService.GetOptimizationRunsPagedAsync(
            new OptimizationFilterRequest(Page: PageNumber, PageSize: 10),
            cancellationToken);

        if (RunId.HasValue)
        {
            SelectedRun = await _optimizationService.GetOptimizationRunByIdAsync(RunId.Value, cancellationToken);
        }
        else if (Runs.Items.Count > 0)
        {
            SelectedRun = Runs.Items.First();
        }
    }

    public async Task<IActionResult> OnPostSolveAsync(
        OptimizationObjective objective,
        string depotAddress,
        double depotLatitude,
        double depotLongitude,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create a realistic sample set of multi-stop delivery jobs across metro area
            var sampleStops = new List<StopInputRequest>
            {
                new("Downtown Tech Center", "100 Market St, San Francisco, CA", 37.7937, -122.3965, 45m, "Alice Walker (555-0101)"),
                new("Mission District Hub", "2450 Mission St, San Francisco, CA", 37.7587, -122.4190, 80m, "Carlos Mendez (555-0102)"),
                new("Sunset Retail Galleria", "1200 Irving St, San Francisco, CA", 37.7638, -122.4712, 120m, "Sarah Jenkins (555-0103)"),
                new("Marina Bay Gourmet", "2100 Chestnut St, San Francisco, CA", 37.8005, -122.4370, 65m, "David Kim (555-0104)"),
                new("Potrero Hill Logistics", "300 16th St, San Francisco, CA", 37.7668, -122.3912, 110m, "Emma Stone (555-0105)"),
                new("SOMA Commerce Park", "650 Townsend St, San Francisco, CA", 37.7712, -122.4034, 95m, "Michael Chang (555-0106)"),
                new("Richmond Health Clinic", "350 Clement St, San Francisco, CA", 37.7831, -122.4635, 30m, "Dr. Lisa Ray (555-0107)")
            };

            var request = new CreateOptimizationRunRequest(
                Objective: objective,
                Stops: sampleStops,
                DepotAddress: string.IsNullOrWhiteSpace(depotAddress) ? "Central Distribution Warehouse" : depotAddress,
                DepotLatitude: depotLatitude != 0 ? depotLatitude : 37.7749,
                DepotLongitude: depotLongitude != 0 ? depotLongitude : -122.4194);

            var run = await _optimizationService.SolveVrpAsync(request, cancellationToken);
            SuccessMessage = $"Route optimization run '{run.RunNumber}' solved successfully! {run.VehiclesAllocated} vehicles assigned across {run.TotalStopsInput} stops.";
            return RedirectToPage(new { RunId = run.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDispatchAsync(Guid runId, CancellationToken cancellationToken)
    {
        try
        {
            var run = await _optimizationService.DispatchRunAsync(new DispatchOptimizationRunRequest(runId), cancellationToken);
            SuccessMessage = $"Optimization run '{run.RunNumber}' dispatched to active driver devices and telematics units.";
            return RedirectToPage(new { RunId = run.Id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { RunId = runId });
        }
    }
}
