using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Billing.Invoices;

public sealed class DetailsModel : PageModel
{
    private readonly IInvoiceService _invoiceService;
    private readonly IPaymentService _paymentService;

    public DetailsModel(
        IInvoiceService invoiceService,
        IPaymentService paymentService)
    {
        _invoiceService = invoiceService;
        _paymentService = paymentService;
    }

    public InvoiceDto Invoice { get; private set; } = null!;
    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(id, HttpContext.RequestAborted);
            if (result is null)
            {
                return NotFound();
            }

            Invoice = result;
            return Page();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostRecordPaymentAsync(
        Guid invoiceId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? notes,
        string? lastFourDigits,
        string? cardBrand)
    {
        try
        {
            var request = new RecordPaymentRequest
            {
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                Notes = notes,
                LastFourDigits = lastFourDigits,
                CardBrand = cardBrand
            };

            await _paymentService.RecordPaymentAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = $"Payment of ${amount:N2} recorded and applied to invoice.";
            return RedirectToPage(new { id = invoiceId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { id = invoiceId });
        }
    }

    public async Task<IActionResult> OnPostIssueAsync(Guid invoiceId)
    {
        try
        {
            await _invoiceService.IssueInvoiceAsync(invoiceId, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Invoice issued and posted to General Ledger.";
            return RedirectToPage(new { id = invoiceId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { id = invoiceId });
        }
    }

    public async Task<IActionResult> OnPostVoidAsync(Guid invoiceId, string? reason)
    {
        try
        {
            await _invoiceService.VoidInvoiceAsync(invoiceId, reason, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Invoice has been voided.";
            return RedirectToPage(new { id = invoiceId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage(new { id = invoiceId });
        }
    }
}
