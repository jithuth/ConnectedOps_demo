using ConnectedOps.Application.Billing;
using ConnectedOps.Domain.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Billing.Payments;

public sealed class IndexModel : PageModel
{
    private readonly IPaymentService _paymentService;

    public IndexModel(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public IReadOnlyCollection<PaymentTransactionDto> Payments { get; private set; } = [];
    public decimal TotalCollected => Payments.Where(p => p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount);
    public decimal BankTransferTotal => Payments.Where(p => p.PaymentMethod == PaymentMethod.BankTransfer && p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount);
    public decimal CardTotal => Payments.Where(p => p.PaymentMethod == PaymentMethod.CreditCard && p.Status == PaymentStatus.Succeeded).Sum(p => p.Amount);
    public decimal RefundedTotal => Payments.Where(p => p.Status == PaymentStatus.Refunded).Sum(p => p.Amount);

    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Payments = await _paymentService.GetPaymentsAsync(HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    public async Task<IActionResult> OnPostRefundAsync(Guid paymentId, string? reason)
    {
        try
        {
            await _paymentService.RefundPaymentAsync(paymentId, reason, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Payment has been refunded and ledger reversed.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }
}
