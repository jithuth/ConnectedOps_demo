using ConnectedOps.Application.Fuel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Fuel.Transactions;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly IFuelTransactionService _transactionService;
    private readonly IFuelAnomalyService _anomalyService;

    public DetailsModel(
        IFuelTransactionService transactionService,
        IFuelAnomalyService anomalyService)
    {
        _transactionService = transactionService;
        _anomalyService = anomalyService;
    }

    public FuelTransactionDto Transaction { get; private set; } = null!;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            Transaction = await _transactionService.GetByIdAsync(id);
            return Page();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, [FromForm] string? cancellationReason)
    {
        try
        {
            await _transactionService.CancelAsync(id, new CancelFuelTransactionRequest(cancellationReason));
            StatusMessage = "Fuel transaction has been cancelled.";
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostAddDocumentAsync(
        Guid id,
        [FromForm] AddFuelTransactionDocumentRequest documentRequest)
    {
        try
        {
            await _transactionService.AddDocumentAsync(id, documentRequest);
            StatusMessage = "Document attached successfully.";
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostDeleteDocumentAsync(Guid id, Guid documentId)
    {
        try
        {
            await _transactionService.DeleteDocumentAsync(id, documentId);
            StatusMessage = "Document removed.";
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostResolveAnomalyAsync(Guid id, Guid anomalyId, [FromForm] string? resolutionNotes)
    {
        try
        {
            await _anomalyService.ResolveAsync(anomalyId, new ResolveFuelAnomalyRequest(resolutionNotes));
            StatusMessage = "Fuel anomaly resolved.";
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostDismissAnomalyAsync(Guid id, Guid anomalyId, [FromForm] string? dismissalReason)
    {
        try
        {
            await _anomalyService.DismissAsync(anomalyId, new DismissFuelAnomalyRequest(dismissalReason));
            StatusMessage = "Fuel anomaly dismissed.";
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage("/Fuel/Transactions/Details", new { id });
        }
    }
}
