using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleModel : BaseEntity
{
    private VehicleModel()
    {
    }

    public VehicleModel(
        Guid tenantId,
        Guid vehicleMakeId,
        string name,
        Guid? defaultCategoryId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (vehicleMakeId == Guid.Empty)
            throw new ArgumentException("VehicleMakeId is required.", nameof(vehicleMakeId));

        TenantId = tenantId;
        VehicleMakeId = vehicleMakeId;
        SetName(name);
        DefaultCategoryId = defaultCategoryId;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid VehicleMakeId { get; private set; }
    public VehicleMake VehicleMake { get; private set; } = null!;
    public string Name { get; private set; } = string.Empty;
    public Guid? DefaultCategoryId { get; private set; }
    public VehicleCategory? DefaultCategory { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, Guid? defaultCategoryId)
    {
        SetName(name);
        DefaultCategoryId = defaultCategoryId;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Model name is required.", nameof(name));

        Name = name.Trim();
    }
}
