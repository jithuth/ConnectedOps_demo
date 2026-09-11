using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Vehicles;

public sealed class VehicleCategory : BaseEntity
{
    private VehicleCategory()
    {
    }

    public VehicleCategory(
        Guid tenantId,
        string name,
        string code,
        string? description = null,
        bool isMotorized = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        SetCode(code);
        Description = description?.Trim();
        IsMotorized = isMotorized;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsMotorized { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string name, string? description, bool isMotorized)
    {
        SetName(name);
        Description = description?.Trim();
        IsMotorized = isMotorized;
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
            throw new ArgumentException("Category name is required.", nameof(name));

        Name = name.Trim();
    }

    private void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Category code is required.", nameof(code));

        Code = code.Trim().ToUpperInvariant();
    }
}
