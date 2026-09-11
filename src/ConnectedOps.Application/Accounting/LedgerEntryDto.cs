namespace ConnectedOps.Application.Accounting;

public sealed record LedgerEntryDto(
    Guid Id,
    Guid TenantId,
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string EntryNumber,
    DateTime EntryDateUtc,
    decimal DebitAmount,
    decimal CreditAmount,
    string Description,
    string? ReferenceType,
    Guid? ReferenceId,
    string Currency,
    DateTime CreatedAtUtc);

public sealed record CreateJournalEntryRequest
{
    public DateTime EntryDateUtc { get; init; } = DateTime.UtcNow;
    public string Description { get; init; } = string.Empty;
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string Currency { get; init; } = "USD";
    public List<JournalEntryLineRequest> Lines { get; init; } = [];
}

public sealed record JournalEntryLineRequest
{
    public Guid AccountId { get; init; }
    public decimal DebitAmount { get; init; }
    public decimal CreditAmount { get; init; }
    public string? Description { get; init; }
}

public sealed record FinancialSummaryDto(
    decimal TotalRevenue,
    decimal TotalAccountsReceivable,
    decimal TotalCashAndBank,
    decimal TotalExpenses,
    decimal NetIncome,
    IReadOnlyCollection<AccountBalanceDto> RevenueAccounts,
    IReadOnlyCollection<AccountBalanceDto> ExpenseAccounts,
    IReadOnlyCollection<LedgerEntryDto> RecentEntries);

public sealed record AccountBalanceDto(
    string AccountCode,
    string AccountName,
    decimal Balance);
