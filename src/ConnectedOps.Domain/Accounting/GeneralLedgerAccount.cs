using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Accounting;

public sealed class GeneralLedgerAccount : BaseEntity
{
    private readonly List<LedgerEntry> _entries = [];

    private GeneralLedgerAccount()
    {
    }

    public GeneralLedgerAccount(
        Guid tenantId,
        string accountCode,
        string accountName,
        AccountCategory category,
        string? description = null,
        bool isSystem = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(accountCode))
            throw new ArgumentException("Account code is required.", nameof(accountCode));
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));

        TenantId = tenantId;
        AccountCode = accountCode.Trim().ToUpperInvariant();
        AccountName = accountName.Trim();
        Category = category;
        Description = description?.Trim();
        IsSystem = isSystem;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string AccountCode { get; private set; } = string.Empty;
    public string AccountName { get; private set; } = string.Empty;
    public AccountCategory Category { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsSystem { get; private set; }

    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    public void UpdateDetails(string accountName, string? description)
    {
        if (string.IsNullOrWhiteSpace(accountName))
            throw new ArgumentException("Account name is required.", nameof(accountName));

        AccountName = accountName.Trim();
        Description = description?.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        if (IsSystem)
            throw new InvalidOperationException("System accounts cannot be deactivated.");

        IsActive = false;
        MarkUpdated();
    }
}
