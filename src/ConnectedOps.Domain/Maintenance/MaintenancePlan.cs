using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Maintenance;

public sealed class MaintenancePlan : BaseEntity
{
    private readonly List<MaintenancePlanRule> _rules = [];
    private readonly List<VehicleMaintenancePlanAssignment> _assignments = [];

    private MaintenancePlan()
    {
    }

    public MaintenancePlan(
        Guid tenantId,
        string code,
        string name,
        string? description = null,
        Guid? vehicleCategoryId = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        VehicleCategoryId = vehicleCategoryId;
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? VehicleCategoryId { get; private set; }
    public VehicleCategory? VehicleCategory { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<MaintenancePlanRule> Rules => _rules;
    public IReadOnlyCollection<VehicleMaintenancePlanAssignment> Assignments => _assignments;

    public void Update(
        string code,
        string name,
        string? description,
        Guid? vehicleCategoryId,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Description = description?.Trim();
        VehicleCategoryId = vehicleCategoryId;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
