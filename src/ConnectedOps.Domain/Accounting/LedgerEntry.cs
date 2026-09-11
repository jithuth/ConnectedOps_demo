using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Accounting;

public sealed class LedgerEntry : BaseEntity
{
    private LedgerEntry()
    {
    }

    public LedgerEntry(
        Guid tenantId,
        Guid accountId,
        string entryNumber,
        DateTime entryDateUtc,
        decimal debitAmount,
        decimal creditAmount,
        string description,
        string? referenceType = null,
        Guid? referenceId = null,
        string currency = "USD")
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (accountId == Guid.Empty)
            throw new ArgumentException("AccountId is required.", nameof(accountId));
        if (string.IsNullOrWhiteSpace(entryNumber))
            throw new ArgumentException("Entry number is required.", nameof(entryNumber));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (debitAmount < 0 || creditAmount < 0)
            throw new ArgumentException("Debit and Credit amounts cannot be negative.");
        if (debitAmount == 0 && creditAmount == 0)
            throw new ArgumentException("Entry must have either a Debit or Credit amount greater than zero.");

        TenantId = tenantId;
        AccountId = accountId;
        EntryNumber = entryNumber.Trim().ToUpperInvariant();
        EntryDateUtc = entryDateUtc;
        DebitAmount = debitAmount;
        CreditAmount = creditAmount;
        Description = description.Trim();
        ReferenceType = referenceType?.Trim();
        ReferenceId = referenceId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
    }

    public Guid TenantId { get; private set; }
    public Guid AccountId { get; private set; }
    public GeneralLedgerAccount Account { get; private set; } = null!;

    public string EntryNumber { get; private set; } = string.Empty;
    public DateTime EntryDateUtc { get; private set; }
    public decimal DebitAmount { get; private set; }
    public decimal CreditAmount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public string Currency { get; private set; } = "USD";
}
