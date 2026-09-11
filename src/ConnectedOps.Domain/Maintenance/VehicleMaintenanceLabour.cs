using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceLabour : BaseEntity
{
    private VehicleMaintenanceLabour()
    {
    }

    public VehicleMaintenanceLabour(
        Guid tenantId,
        Guid maintenanceRecordId,
        string description,
        decimal hours,
        decimal? hourlyRate = null,
        string? technicianName = null,
        Guid? employeeId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (hours <= 0)
            throw new ArgumentException("Hours must be greater than zero.", nameof(hours));
        if (hourlyRate.HasValue && hourlyRate.Value < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));

        TenantId = tenantId;
        MaintenanceRecordId = maintenanceRecordId;
        Description = description.Trim();
        Hours = hours;
        HourlyRate = hourlyRate;
        TotalCost = hourlyRate.HasValue ? hours * hourlyRate.Value : null;
        TechnicianName = technicianName?.Trim();
        EmployeeId = employeeId;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord MaintenanceRecord { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;
    public decimal Hours { get; private set; }
    public decimal? HourlyRate { get; private set; }
    public decimal? TotalCost { get; private set; }

    public string? TechnicianName { get; private set; }
    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        string description,
        decimal hours,
        decimal? hourlyRate,
        string? technicianName,
        Guid? employeeId,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (hours <= 0)
            throw new ArgumentException("Hours must be greater than zero.", nameof(hours));
        if (hourlyRate.HasValue && hourlyRate.Value < 0)
            throw new ArgumentException("Hourly rate cannot be negative.", nameof(hourlyRate));

        Description = description.Trim();
        Hours = hours;
        HourlyRate = hourlyRate;
        TotalCost = hourlyRate.HasValue ? hours * hourlyRate.Value : null;
        TechnicianName = technicianName?.Trim();
        EmployeeId = employeeId;
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
