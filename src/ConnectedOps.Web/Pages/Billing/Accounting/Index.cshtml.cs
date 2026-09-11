using ConnectedOps.Application.Accounting;
using ConnectedOps.Domain.Accounting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ConnectedOps.Web.Pages.Billing.Accounting;

public sealed class IndexModel : PageModel
{
    private readonly IAccountingLedgerService _ledgerService;

    public IndexModel(IAccountingLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    public FinancialSummaryDto Summary { get; private set; } = null!;
    public IReadOnlyCollection<GeneralLedgerAccountDto> Accounts { get; private set; } = [];
    public IReadOnlyCollection<LedgerEntryDto> Entries { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync()
    {
        try
        {
            Summary = await _ledgerService.GetFinancialSummaryAsync(HttpContext.RequestAborted);
            Accounts = await _ledgerService.GetAccountsAsync(HttpContext.RequestAborted);
            Entries = await _ledgerService.GetEntriesAsync(null, HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Summary = new FinancialSummaryDto(0, 0, 0, 0, 0, [], [], []);
        }
    }

    public async Task<IActionResult> OnPostCreateAccountAsync(
        string accountCode,
        string accountName,
        AccountCategory category,
        string? description)
    {
        try
        {
            var request = new CreateGeneralLedgerAccountRequest
            {
                AccountCode = accountCode,
                AccountName = accountName,
                Category = category,
                Description = description
            };

            await _ledgerService.CreateAccountAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = $"Account '{accountCode} - {accountName}' created in Chart of Accounts.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostCreateJournalEntryAsync(
        string description,
        Guid debitAccountId,
        decimal amount,
        Guid creditAccountId,
        string? reference)
    {
        try
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Journal entry amount must be greater than zero.");
            }

            if (debitAccountId == creditAccountId)
            {
                throw new InvalidOperationException("Debit account and Credit account must be different.");
            }

            var lines = new List<JournalEntryLineRequest>
            {
                new() { AccountId = debitAccountId, DebitAmount = amount, CreditAmount = 0, Description = description },
                new() { AccountId = creditAccountId, DebitAmount = 0, CreditAmount = amount, Description = description }
            };

            var request = new CreateJournalEntryRequest
            {
                Description = description,
                ReferenceType = reference,
                EntryDateUtc = DateTime.UtcNow,
                Lines = lines
            };

            await _ledgerService.RecordJournalEntryAsync(request, HttpContext.RequestAborted);
            TempData["SuccessMessage"] = "Journal entry posted successfully with balanced debits and credits.";
            return RedirectToPage();
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToPage();
        }
    }
}
