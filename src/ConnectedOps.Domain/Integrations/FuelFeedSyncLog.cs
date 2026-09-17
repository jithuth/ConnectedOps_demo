using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Integrations;

public sealed class FuelFeedSyncLog : BaseEntity
{
    private FuelFeedSyncLog()
    {
    }

    public FuelFeedSyncLog(
        Guid tenantId,
        FuelClearinghouseProvider provider,
        int transactionsCount,
        decimal totalSpend,
        decimal totalLiters,
        string status,
        string? errorMessage = null,
        string? rawSummary = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        Provider = provider;
        SyncedAtUtc = DateTime.UtcNow;
        TransactionsCount = Math.Max(0, transactionsCount);
        TotalSpend = totalSpend;
        TotalLiters = totalLiters;
        Status = string.IsNullOrWhiteSpace(status) ? "Completed" : status.Trim();
        ErrorMessage = errorMessage?.Trim();
        RawSummary = rawSummary?.Trim();
    }

    public Guid TenantId { get; private set; }
    public FuelClearinghouseProvider Provider { get; private set; }
    public DateTime SyncedAtUtc { get; private set; }
    public int TransactionsCount { get; private set; }
    public decimal TotalSpend { get; private set; }
    public decimal TotalLiters { get; private set; }
    public string Status { get; private set; } = "Completed";
    public string? ErrorMessage { get; private set; }
    public string? RawSummary { get; private set; }
}
