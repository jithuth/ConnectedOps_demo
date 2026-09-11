namespace ConnectedOps.Application.Accounting;

public interface IAccountingLedgerService
{
    Task<IReadOnlyCollection<GeneralLedgerAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default);
    Task<GeneralLedgerAccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GeneralLedgerAccountDto> CreateAccountAsync(CreateGeneralLedgerAccountRequest request, CancellationToken cancellationToken = default);
    Task<GeneralLedgerAccountDto> UpdateAccountAsync(Guid id, UpdateGeneralLedgerAccountRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LedgerEntryDto>> GetEntriesAsync(Guid? accountId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LedgerEntryDto>> RecordJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default);
    Task<FinancialSummaryDto> GetFinancialSummaryAsync(CancellationToken cancellationToken = default);
    Task EnsureDefaultAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
