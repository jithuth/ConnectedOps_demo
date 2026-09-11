using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelTypeDefinition : BaseEntity
{
    private FuelTypeDefinition()
    {
    }

    public FuelTypeDefinition(
        Guid? tenantId,
        string code,
        string name,
        FuelType fuelType,
        string energyType = "Liquid",
        FuelUnit defaultUnit = FuelUnit.Liter,
        decimal? density = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        FuelType = fuelType;
        EnergyType = energyType.Trim();
        DefaultUnit = defaultUnit;
        Density = density;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public FuelType FuelType { get; private set; }
    public string EnergyType { get; private set; } = "Liquid";
    public FuelUnit DefaultUnit { get; private set; } = FuelUnit.Liter;
    public decimal? Density { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        FuelType fuelType,
        string energyType,
        FuelUnit defaultUnit,
        decimal? density,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        FuelType = fuelType;
        EnergyType = energyType.Trim();
        DefaultUnit = defaultUnit;
        Density = density;
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
