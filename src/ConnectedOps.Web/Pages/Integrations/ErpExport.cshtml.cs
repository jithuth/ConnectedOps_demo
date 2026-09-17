using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Integrations;

[Authorize]
public class ErpExportModel : PageModel
{
    private readonly IErpExportService _erpService;

    public ErpExportModel(IErpExportService erpService)
    {
        _erpService = erpService;
    }

    public PagedResult<ErpExportBatchDto> Batches { get; private set; } = null!;

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Batches = await _erpService.GetBatchesPagedAsync(PageNumber, pageSize: 15, cancellationToken);
    }

    public async Task<IActionResult> OnPostGenerateAsync(
        ErpTargetSystem targetSystem,
        ErpBatchType batchType,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            if (periodEndUtc <= periodStartUtc)
            {
                ErrorMessage = "Period end date must be after period start date.";
                return RedirectToPage();
            }

            var batch = await _erpService.GenerateBatchAsync(
                new GenerateErpBatchRequest(targetSystem, batchType, periodStartUtc, periodEndUtc),
                cancellationToken);

            SuccessMessage = $"Export Batch {batch.BatchNumber} generated successfully with {batch.RecordCount} records totalling {batch.TotalAmount:N2} {batch.Currency}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkExportedAsync(Guid batchId, string? externalReference, CancellationToken cancellationToken)
    {
        try
        {
            await _erpService.MarkBatchExportedAsync(batchId, externalReference, cancellationToken);
            SuccessMessage = "Batch marked as Exported / Synchronized.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage();
    }
}
