using ConnectedOps.Application.Accounting;
using ConnectedOps.Domain.Authorization;
using ConnectedOps.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConnectedOps.Api.Controllers.Accounting;

[ApiController]
[Route("api/accounting")]
[Authorize]
public sealed class AccountingController : ControllerBase
{
    private readonly IAccountingLedgerService _ledgerService;

    public AccountingController(IAccountingLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("accounts")]
    [RequirePermission(PermissionKeys.Accounting.View)]
    public async Task<ActionResult<IReadOnlyCollection<GeneralLedgerAccountDto>>> GetAccounts(
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.GetAccountsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("accounts/{id:guid}")]
    [RequirePermission(PermissionKeys.Accounting.View)]
    public async Task<ActionResult<GeneralLedgerAccountDto>> GetAccountById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.GetAccountByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPost("accounts")]
    [RequirePermission(PermissionKeys.Accounting.Manage)]
    public async Task<ActionResult<GeneralLedgerAccountDto>> CreateAccount(
        [FromBody] CreateGeneralLedgerAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.CreateAccountAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAccountById), new { id = result.Id }, result);
    }

    [HttpPut("accounts/{id:guid}")]
    [RequirePermission(PermissionKeys.Accounting.Manage)]
    public async Task<ActionResult<GeneralLedgerAccountDto>> UpdateAccount(
        Guid id,
        [FromBody] UpdateGeneralLedgerAccountRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.UpdateAccountAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("entries")]
    [RequirePermission(PermissionKeys.Accounting.View)]
    public async Task<ActionResult<IReadOnlyCollection<LedgerEntryDto>>> GetLedgerEntries(
        [FromQuery] Guid? accountId,
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.GetEntriesAsync(accountId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("journal-entries")]
    [RequirePermission(PermissionKeys.Accounting.Manage)]
    public async Task<ActionResult<IReadOnlyCollection<LedgerEntryDto>>> CreateJournalEntry(
        [FromBody] CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.RecordJournalEntryAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    [RequirePermission(PermissionKeys.Accounting.View)]
    public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary(
        CancellationToken cancellationToken)
    {
        var result = await _ledgerService.GetFinancialSummaryAsync(cancellationToken);
        return Ok(result);
    }
}
