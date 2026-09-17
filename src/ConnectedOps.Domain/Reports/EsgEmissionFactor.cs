using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Reports;

public sealed class EsgEmissionFactor : BaseEntity
{
    private EsgEmissionFactor()
    {
    }

    public EsgEmissionFactor(
        Guid? tenantId,
        string fuelCode,
        string fuelName,
        FuelType fuelType,
        decimal kgCo2PerUnit,
        string unitName,
        string regulatoryStandard = "EPA / GHG Protocol",
        bool isDefault = true)
    {
        if (string.IsNullOrWhiteSpace(fuelCode))
            throw new ArgumentException("FuelCode is required.", nameof(fuelCode));
        if (string.IsNullOrWhiteSpace(fuelName))
            throw new ArgumentException("FuelName is required.", nameof(fuelName));

        TenantId = tenantId;
        FuelCode = fuelCode.Trim().ToUpperInvariant();
        FuelName = fuelName.Trim();
        FuelType = fuelType;
        KgCo2PerUnit = kgCo2PerUnit;
        UnitName = unitName.Trim();
        RegulatoryStandard = regulatoryStandard.Trim();
        IsDefault = isDefault;
    }

    public Guid? TenantId { get; private set; }
    public string FuelCode { get; private set; } = string.Empty;
    public string FuelName { get; private set; } = string.Empty;
    public FuelType FuelType { get; private set; }
    public decimal KgCo2PerUnit { get; private set; }
    public string UnitName { get; private set; } = "Liter";
    public string RegulatoryStandard { get; private set; } = "EPA / GHG Protocol";
    public bool IsDefault { get; private set; }

    public void Update(
        decimal kgCo2PerUnit,
        string unitName,
        string regulatoryStandard,
        Guid? updatedBy = null)
    {
        KgCo2PerUnit = kgCo2PerUnit;
        UnitName = unitName.Trim();
        RegulatoryStandard = regulatoryStandard.Trim();
        MarkUpdated(updatedBy);
    }
}
