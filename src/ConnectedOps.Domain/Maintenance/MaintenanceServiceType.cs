using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class MaintenanceServiceType : BaseEntity
{
    private MaintenanceServiceType()
    {
    }

    public MaintenanceServiceType(
        Guid tenantId,
        string code,
        string name,
        MaintenanceServiceCategory category = MaintenanceServiceCategory.Preventive,
        string? description = null,
        decimal? defaultDurationHours = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (defaultDurationHours.HasValue && defaultDurationHours.Value < 0)
            throw new ArgumentException("Default duration cannot be negative.", nameof(defaultDurationHours));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        Description = description?.Trim();
        DefaultDurationHours = defaultDurationHours;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public MaintenanceServiceCategory Category { get; private set; }
    public decimal? DefaultDurationHours { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        MaintenanceServiceCategory category,
        string? description,
        decimal? defaultDurationHours,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (defaultDurationHours.HasValue && defaultDurationHours.Value < 0)
            throw new ArgumentException("Default duration cannot be negative.", nameof(defaultDurationHours));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Category = category;
        Description = description?.Trim();
        DefaultDurationHours = defaultDurationHours;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
