using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceTask : BaseEntity
{
    private VehicleMaintenanceTask()
    {
    }

    public VehicleMaintenanceTask(
        Guid tenantId,
        Guid maintenanceRecordId,
        string name,
        Guid? maintenanceServiceTypeId = null,
        string? description = null,
        MaintenanceTaskStatus status = MaintenanceTaskStatus.Pending,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name is required.", nameof(name));

        TenantId = tenantId;
        MaintenanceRecordId = maintenanceRecordId;
        MaintenanceServiceTypeId = maintenanceServiceTypeId;
        Name = name.Trim();
        Description = description?.Trim();
        Status = status;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord MaintenanceRecord { get; private set; } = null!;

    public Guid? MaintenanceServiceTypeId { get; private set; }
    public MaintenanceServiceType? MaintenanceServiceType { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MaintenanceTaskStatus Status { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public Guid? CompletedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public void UpdateStatus(MaintenanceTaskStatus status, Guid? completedByUserId = null, string? notes = null)
    {
        Status = status;
        if (status == MaintenanceTaskStatus.Completed)
        {
            CompletedAtUtc = DateTime.UtcNow;
            CompletedByUserId = completedByUserId;
        }
        else
        {
            CompletedAtUtc = null;
            CompletedByUserId = null;
        }
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes.Trim();
        }
        MarkUpdated(completedByUserId);
    }
}
