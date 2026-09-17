using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Integrations;

[Authorize]
public class FuelFeedsModel : PageModel
{
    private readonly IFuelClearinghouseService _fuelService;

    public FuelFeedsModel(IFuelClearinghouseService fuelService)
    {
        _fuelService = fuelService;
    }

    public IntegrationsDashboardDto Dashboard { get; private set; } = null!;
    public PagedResult<FuelFeedSyncLogDto> SyncLogs { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Dashboard = await _fuelService.GetDashboardAsync(cancellationToken);
        SyncLogs = await _fuelService.GetSyncLogsPagedAsync(PageNumber, pageSize: 15, cancellationToken);
    }

    public async Task<IActionResult> OnPostSyncAsync(FuelClearinghouseProvider provider, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _fuelService.TriggerSyncAsync(
                new TriggerFuelSyncRequest(provider, SimulateTransactions: true),
                cancellationToken);

            SuccessMessage = $"Clearinghouse sync completed for {result.ProviderName}: Ingested {result.TransactionsCount} transactions totalling ${result.TotalSpend:N2} ({result.TotalLiters:N2} L).";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
