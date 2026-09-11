using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceExpense : BaseEntity
{
    private VehicleMaintenanceExpense()
    {
    }

    public VehicleMaintenanceExpense(
        Guid tenantId,
        Guid maintenanceRecordId,
        MaintenanceExpenseType expenseType,
        string description,
        decimal amount,
        string? currencyCode = "USD",
        string? reference = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        TenantId = tenantId;
        MaintenanceRecordId = maintenanceRecordId;
        ExpenseType = expenseType;
        Description = description.Trim();
        Amount = amount;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord MaintenanceRecord { get; private set; } = null!;

    public MaintenanceExpenseType ExpenseType { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        MaintenanceExpenseType expenseType,
        string description,
        decimal amount,
        string? currencyCode,
        string? reference,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));

        ExpenseType = expenseType;
        Description = description.Trim();
        Amount = amount;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        Reference = reference?.Trim();
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
