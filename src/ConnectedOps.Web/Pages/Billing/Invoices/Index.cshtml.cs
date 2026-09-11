using ConnectedOps.Application.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Billing.Invoices;

public sealed class IndexModel : PageModel
{
    private readonly IInvoiceService _invoiceService;

    public IndexModel(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    public IReadOnlyCollection<InvoiceListItemDto> Invoices { get; private set; } = [];
    public decimal TotalInvoiced => Invoices.Where(i => i.Status != Domain.Billing.InvoiceStatus.Void).Sum(i => i.TotalAmount);
    public decimal TotalPaid => Invoices.Where(i => i.Status != Domain.Billing.InvoiceStatus.Void).Sum(i => i.AmountPaid);
    public decimal TotalOutstanding => Invoices.Where(i => i.Status != Domain.Billing.InvoiceStatus.Void).Sum(i => i.BalanceDue);
    public int OverdueCount => Invoices.Count(i => i.Status == Domain.Billing.InvoiceStatus.Overdue || (i.DueDateUtc < DateTime.UtcNow && i.BalanceDue > 0 && i.Status != Domain.Billing.InvoiceStatus.Void));

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Invoices = await _invoiceService.GetInvoicesAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string title,
        DateTime dueDateUtc,
        string currency,
        string? billingContactName,
        string? billingEmail,
        string? billingAddress,
        string? notes,
        string? termsAndConditions,
        List<string> itemDescription,
        List<decimal> itemQuantity,
        List<decimal> itemUnitPrice,
        List<decimal> itemTaxRate)
    {
        try
        {
            var items = new List<CreateInvoiceItemRequest>();
            for (var i = 0; i < itemDescription.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(itemDescription[i]))
                {
                    items.Add(new CreateInvoiceItemRequest
                    {
                        Description = itemDescription[i].Trim(),
                        Quantity = itemQuantity.Count > i ? itemQuantity[i] : 1,
                        UnitPrice = itemUnitPrice.Count > i ? itemUnitPrice[i] : 0,
                        TaxRatePercentage = itemTaxRate.Count > i ? itemTaxRate[i] : 0
                    });
                }
            }

            var request = new CreateInvoiceRequest
            {
                Title = title,
                DueDateUtc = dueDateUtc,
                Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
                BillingContactName = billingContactName,
                BillingEmail = billingEmail,
                BillingAddress = billingAddress,
                Notes = notes,
                TermsAndConditions = termsAndConditions,
                Items = items
            };

            await _invoiceService.CreateInvoiceAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Invoice created successfully as Draft.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostIssueAsync(Guid invoiceId)
    {
        try
        {
            await _invoiceService.IssueInvoiceAsync(invoiceId, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Invoice issued and posted to General Ledger.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostVoidAsync(Guid invoiceId, string? reason)
    {
        try
        {
            await _invoiceService.VoidInvoiceAsync(invoiceId, reason, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Invoice has been voided.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }
}
