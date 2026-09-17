using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Integrations;

public sealed class ErpExportBatch : BaseEntity
{
    private ErpExportBatch()
    {
    }

    public ErpExportBatch(
        Guid tenantId,
        string batchNumber,
        ErpTargetSystem targetSystem,
        ErpBatchType batchType,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        int recordCount,
        decimal totalAmount,
        string payloadJson,
        string currency = "AED",
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new ArgumentException("BatchNumber is required.", nameof(batchNumber));

        TenantId = tenantId;
        BatchNumber = batchNumber.Trim().ToUpperInvariant();
        TargetSystem = targetSystem;
        BatchType = batchType;
        PeriodStartUtc = periodStartUtc;
        PeriodEndUtc = periodEndUtc;
        RecordCount = recordCount;
        TotalAmount = totalAmount;
        PayloadJson = payloadJson ?? "{}";
        Currency = string.IsNullOrWhiteSpace(currency) ? "AED" : currency.Trim().ToUpperInvariant();
        Status = ErpBatchStatus.Generated;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string BatchNumber { get; private set; } = string.Empty;
    public ErpTargetSystem TargetSystem { get; private set; }
    public ErpBatchType BatchType { get; private set; }
    public ErpBatchStatus Status { get; private set; }
    public DateTime PeriodStartUtc { get; private set; }
    public DateTime PeriodEndUtc { get; private set; }
    public int RecordCount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "AED";
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime? ExportedAtUtc { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? ErrorMessage { get; private set; }

    public void MarkExported(string? externalRef = null, Guid? updatedBy = null)
    {
        Status = ErpBatchStatus.Exported;
        ExportedAtUtc = DateTime.UtcNow;
        ExternalReference = externalRef?.Trim();
        MarkUpdated(updatedBy);
    }

    public void MarkAcknowledged(string? externalRef = null, Guid? updatedBy = null)
    {
        Status = ErpBatchStatus.Acknowledged;
        if (!string.IsNullOrWhiteSpace(externalRef))
            ExternalReference = externalRef.Trim();
        MarkUpdated(updatedBy);
    }

    public void MarkFailed(string error, Guid? updatedBy = null)
    {
        Status = ErpBatchStatus.Failed;
        ErrorMessage = error?.Trim();
        MarkUpdated(updatedBy);
    }
}
