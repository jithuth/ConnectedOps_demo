using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Anomalies;

[Authorize]
public class IndexModel : PageModel
{
    private readonly IFuelAnomalyService _anomalyService;
    private readonly IVehicleService _vehicleService;

    public IndexModel(
        IFuelAnomalyService anomalyService,
        IVehicleService vehicleService)
    {
        _anomalyService = anomalyService;
        _vehicleService = vehicleService;
    }

    [BindProperty(SupportsGet = true)]
    public FuelAnomalyQueryParameters Query { get; set; } = new();

    public PagedResult<FuelAnomalyDto> Result { get; private set; } = null!;
    public IReadOnlyCollection<VehicleListItemDto> Vehicles { get; private set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        Result = await _anomalyService.GetAnomaliesPagedAsync(Query);
        var vResult = await _vehicleService.GetVehiclesPagedAsync(new VehicleQueryParameters { PageSize = 200 });
        Vehicles = vResult.Items;
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid id, [FromForm] string? resolutionNotes)
    {
        try
        {
            await _anomalyService.ResolveAsync(id, new ResolveFuelAnomalyRequest(resolutionNotes));
            StatusMessage = "Anomaly marked as resolved.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDismissAsync(Guid id, [FromForm] string? dismissalReason)
    {
        try
        {
            await _anomalyService.DismissAsync(id, new DismissFuelAnomalyRequest(dismissalReason));
            StatusMessage = "Anomaly dismissed.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage();
        }
    }
}
