using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelImportBatch : BaseEntity
{
    private readonly List<FuelImportError> _errors = [];

    private FuelImportBatch()
    {
    }

    public FuelImportBatch(
        Guid tenantId,
        FuelTransactionSource source = FuelTransactionSource.Imported,
        string? fileName = null,
        string? providerName = null,
        DateTime? startedAtUtc = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        Source = source;
        FileName = fileName?.Trim();
        ProviderName = providerName?.Trim();
        StartedAtUtc = startedAtUtc ?? DateTime.UtcNow;
        Status = FuelImportStatus.Pending;
        TotalRows = 0;
        ImportedRows = 0;
        RejectedRows = 0;
        CreatedByUserId = createdByUserId;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public FuelTransactionSource Source { get; private set; }
    public string? FileName { get; private set; }
    public string? ProviderName { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int TotalRows { get; private set; }
    public int ImportedRows { get; private set; }
    public int RejectedRows { get; private set; }
    public FuelImportStatus Status { get; private set; }
    public Guid? CreatedByUserId { get; private set; }

    public IReadOnlyCollection<FuelImportError> Errors => _errors.AsReadOnly();

    public void AddError(FuelImportError error)
    {
        _errors.Add(error);
        RejectedRows++;
    }

    public void IncrementImported()
    {
        ImportedRows++;
    }

    public void SetTotalRows(int totalRows)
    {
        TotalRows = totalRows;
    }

    public void Complete(Guid? completedBy = null)
    {
        CompletedAtUtc = DateTime.UtcNow;
        Status = RejectedRows > 0
            ? (ImportedRows > 0 ? FuelImportStatus.CompletedWithErrors : FuelImportStatus.Failed)
            : FuelImportStatus.Completed;
        MarkUpdated(completedBy);
    }
}

public sealed class FuelImportError : BaseEntity
{
    private FuelImportError()
    {
    }

    public FuelImportError(
        Guid tenantId,
        Guid fuelImportBatchId,
        string errorCode,
        string errorMessage,
        int? rowNumber = null,
        string? externalReference = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (fuelImportBatchId == Guid.Empty)
            throw new ArgumentException("FuelImportBatchId is required.", nameof(fuelImportBatchId));
        if (string.IsNullOrWhiteSpace(errorCode))
            throw new ArgumentException("ErrorCode is required.", nameof(errorCode));
        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new ArgumentException("ErrorMessage is required.", nameof(errorMessage));

        TenantId = tenantId;
        FuelImportBatchId = fuelImportBatchId;
        ErrorCode = errorCode.Trim();
        ErrorMessage = errorMessage.Trim();
        RowNumber = rowNumber;
        ExternalReference = externalReference?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FuelImportBatchId { get; private set; }
    public FuelImportBatch FuelImportBatch { get; private set; } = null!;

    public int? RowNumber { get; private set; }
    public string? ExternalReference { get; private set; }
    public string ErrorCode { get; private set; } = string.Empty;
    public string ErrorMessage { get; private set; } = string.Empty;
}
