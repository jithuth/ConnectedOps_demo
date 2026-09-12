using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Compliance;

public sealed class ComplianceRequirementRule : BaseEntity
{
    private ComplianceRequirementRule()
    {
    }

    public ComplianceRequirementRule(
        Guid tenantId,
        Guid complianceRequirementId,
        Guid? vehicleCategoryId = null,
        Guid? assetCategoryId = null,
        DriverType? driverType = null,
        string? countryCode = null,
        Guid? branchId = null,
        bool isActive = true,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (complianceRequirementId == Guid.Empty)
            throw new ArgumentException("ComplianceRequirementId is required.", nameof(complianceRequirementId));

        TenantId = tenantId;
        ComplianceRequirementId = complianceRequirementId;
        VehicleCategoryId = vehicleCategoryId;
        AssetCategoryId = assetCategoryId;
        DriverType = driverType;
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        BranchId = branchId;
        IsActive = isActive;
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid ComplianceRequirementId { get; private set; }
    public ComplianceRequirement ComplianceRequirement { get; private set; } = null!;

    public Guid? VehicleCategoryId { get; private set; }
    public VehicleCategory? VehicleCategory { get; private set; }

    public Guid? AssetCategoryId { get; private set; }
    public AssetCategory? AssetCategory { get; private set; }

    public DriverType? DriverType { get; private set; }

    public string? CountryCode { get; private set; }

    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        Guid? vehicleCategoryId,
        Guid? assetCategoryId,
        DriverType? driverType,
        string? countryCode,
        Guid? branchId,
        bool isActive,
        string? notes,
        Guid? updatedByUserId = null)
    {
        VehicleCategoryId = vehicleCategoryId;
        AssetCategoryId = assetCategoryId;
        DriverType = driverType;
        CountryCode = countryCode?.Trim().ToUpperInvariant();
        BranchId = branchId;
        IsActive = isActive;
        Notes = notes?.Trim();
        UpdatedBy = updatedByUserId;
        MarkUpdated();
    }
}
