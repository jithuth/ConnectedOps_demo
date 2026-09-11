using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenancePart : BaseEntity
{
    private VehicleMaintenancePart()
    {
    }

    public VehicleMaintenancePart(
        Guid tenantId,
        Guid maintenanceRecordId,
        string partName,
        decimal quantity,
        string? partNumber = null,
        string unit = "PCS",
        decimal? unitCost = null,
        string? supplier = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));
        if (string.IsNullOrWhiteSpace(partName))
            throw new ArgumentException("Part name is required.", nameof(partName));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitCost.HasValue && unitCost.Value < 0)
            throw new ArgumentException("Unit cost cannot be negative.", nameof(unitCost));

        TenantId = tenantId;
        MaintenanceRecordId = maintenanceRecordId;
        PartName = partName.Trim();
        Quantity = quantity;
        PartNumber = partNumber?.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? "PCS" : unit.Trim().ToUpperInvariant();
        UnitCost = unitCost;
        TotalCost = unitCost.HasValue ? quantity * unitCost.Value : null;
        Supplier = supplier?.Trim();
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord MaintenanceRecord { get; private set; } = null!;

    public string? PartNumber { get; private set; }
    public string PartName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = "PCS";
    public decimal? UnitCost { get; private set; }
    public decimal? TotalCost { get; private set; }
    public string? Supplier { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        string partName,
        decimal quantity,
        string? partNumber,
        string unit,
        decimal? unitCost,
        string? supplier,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(partName))
            throw new ArgumentException("Part name is required.", nameof(partName));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitCost.HasValue && unitCost.Value < 0)
            throw new ArgumentException("Unit cost cannot be negative.", nameof(unitCost));

        PartName = partName.Trim();
        Quantity = quantity;
        PartNumber = partNumber?.Trim();
        Unit = string.IsNullOrWhiteSpace(unit) ? "PCS" : unit.Trim().ToUpperInvariant();
        UnitCost = unitCost;
        TotalCost = unitCost.HasValue ? quantity * unitCost.Value : null;
        Supplier = supplier?.Trim();
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
