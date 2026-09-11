using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Domain.Assets;

public sealed class Asset : BaseEntity
{
    private readonly List<AssetLocationHistory> _locationHistories = [];
    private readonly List<AssetEmployeeAssignment> _employeeAssignments = [];
    private readonly List<AssetVehicleAssignment> _vehicleAssignments = [];
    private readonly List<AssetUsageSession> _usageSessions = [];
    private readonly List<AssetTransfer> _transfers = [];
    private readonly List<AssetConditionRecord> _conditionRecords = [];
    private readonly List<AssetInspection> _inspections = [];
    private readonly List<AssetCalibrationRecord> _calibrationRecords = [];
    private readonly List<AssetDocument> _documents = [];
    private readonly List<AssetIdentifier> _identifiers = [];
    private readonly List<AssetNote> _notes = [];

    private Asset()
    {
    }

    public Asset(
        Guid tenantId,
        string assetNumber,
        string name,
        Guid assetCategoryId,
        Guid? assetTypeId = null,
        string? internalCode = null,
        string? description = null,
        string? serialNumber = null,
        string? manufacturer = null,
        string? model = null,
        AssetOwnershipType ownershipType = AssetOwnershipType.CompanyOwned,
        AssetStatus status = AssetStatus.Draft,
        AssetCondition condition = AssetCondition.Good,
        Guid? branchId = null,
        Guid? locationId = null,
        Guid? departmentId = null,
        Guid? teamId = null,
        DateOnly? purchaseDate = null,
        decimal? purchaseCost = null,
        string? currencyCode = "USD",
        string? supplierName = null,
        string? purchaseReference = null,
        DateOnly? warrantyStartDate = null,
        DateOnly? warrantyEndDate = null,
        string? warrantyProvider = null,
        string? warrantyReference = null,
        DateTime? commissionedAtUtc = null,
        string? primaryImageObjectKey = null,
        string? notes = null,
        bool isDemo = false,
        bool isActive = true,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(assetNumber))
            throw new ArgumentException("Asset number is required.", nameof(assetNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name is required.", nameof(name));
        if (assetCategoryId == Guid.Empty)
            throw new ArgumentException("AssetCategoryId is required.", nameof(assetCategoryId));

        TenantId = tenantId;
        AssetNumber = assetNumber.Trim().ToUpperInvariant();
        Name = name.Trim();
        AssetCategoryId = assetCategoryId;
        AssetTypeId = assetTypeId;
        InternalCode = string.IsNullOrWhiteSpace(internalCode) ? null : internalCode.Trim().ToUpperInvariant();
        Description = description?.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Manufacturer = manufacturer?.Trim();
        Model = model?.Trim();
        OwnershipType = ownershipType;
        Status = status;
        Condition = condition;
        BranchId = branchId;
        LocationId = locationId;
        DepartmentId = departmentId;
        TeamId = teamId;
        PurchaseDate = purchaseDate;
        PurchaseCost = purchaseCost;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        SupplierName = supplierName?.Trim();
        PurchaseReference = purchaseReference?.Trim();
        WarrantyStartDate = warrantyStartDate;
        WarrantyEndDate = warrantyEndDate;
        WarrantyProvider = warrantyProvider?.Trim();
        WarrantyReference = warrantyReference?.Trim();
        CommissionedAtUtc = commissionedAtUtc;
        PrimaryImageObjectKey = primaryImageObjectKey?.Trim();
        Notes = notes?.Trim();
        IsDemo = isDemo;
        IsActive = isActive;
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public string AssetNumber { get; private set; } = string.Empty;
    public string? InternalCode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid AssetCategoryId { get; private set; }
    public AssetCategory AssetCategory { get; private set; } = null!;
    public Guid? AssetTypeId { get; private set; }
    public AssetType? AssetType { get; private set; }
    public string? SerialNumber { get; private set; }
    public string? Manufacturer { get; private set; }
    public string? Model { get; private set; }
    public AssetOwnershipType OwnershipType { get; private set; }
    public AssetStatus Status { get; private set; }
    public AssetCondition Condition { get; private set; }

    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }
    public Guid? TeamId { get; private set; }
    public Team? Team { get; private set; }

    public Guid? CurrentCustodianEmployeeId { get; private set; }
    public Employee? CurrentCustodianEmployee { get; private set; }
    public Guid? CurrentAssignedVehicleId { get; private set; }
    public Vehicle? CurrentAssignedVehicle { get; private set; }

    public DateOnly? PurchaseDate { get; private set; }
    public decimal? PurchaseCost { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public string? SupplierName { get; private set; }
    public string? PurchaseReference { get; private set; }

    public DateOnly? WarrantyStartDate { get; private set; }
    public DateOnly? WarrantyEndDate { get; private set; }
    public string? WarrantyProvider { get; private set; }
    public string? WarrantyReference { get; private set; }

    public DateTime? CommissionedAtUtc { get; private set; }
    public DateTime? RetiredAtUtc { get; private set; }
    public string? PrimaryImageObjectKey { get; private set; }
    public string? Notes { get; private set; }
    public bool IsDemo { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<AssetLocationHistory> LocationHistories => _locationHistories;
    public IReadOnlyCollection<AssetEmployeeAssignment> EmployeeAssignments => _employeeAssignments;
    public IReadOnlyCollection<AssetVehicleAssignment> VehicleAssignments => _vehicleAssignments;
    public IReadOnlyCollection<AssetUsageSession> UsageSessions => _usageSessions;
    public IReadOnlyCollection<AssetTransfer> Transfers => _transfers;
    public IReadOnlyCollection<AssetConditionRecord> ConditionRecords => _conditionRecords;
    public IReadOnlyCollection<AssetInspection> Inspections => _inspections;
    public IReadOnlyCollection<AssetCalibrationRecord> CalibrationRecords => _calibrationRecords;
    public IReadOnlyCollection<AssetDocument> Documents => _documents;
    public IReadOnlyCollection<AssetIdentifier> Identifiers => _identifiers;
    public IReadOnlyCollection<AssetNote> NotesList => _notes;

    public void UpdateDetails(
        string assetNumber,
        string name,
        Guid assetCategoryId,
        Guid? assetTypeId,
        string? internalCode,
        string? description,
        string? serialNumber,
        string? manufacturer,
        string? model,
        AssetOwnershipType ownershipType,
        Guid? branchId,
        Guid? locationId,
        Guid? departmentId,
        Guid? teamId,
        DateOnly? purchaseDate,
        decimal? purchaseCost,
        string? currencyCode,
        string? supplierName,
        string? purchaseReference,
        DateOnly? warrantyStartDate,
        DateOnly? warrantyEndDate,
        string? warrantyProvider,
        string? warrantyReference,
        DateTime? commissionedAtUtc,
        string? primaryImageObjectKey,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(assetNumber))
            throw new ArgumentException("Asset number is required.", nameof(assetNumber));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name is required.", nameof(name));
        if (assetCategoryId == Guid.Empty)
            throw new ArgumentException("AssetCategoryId is required.", nameof(assetCategoryId));

        AssetNumber = assetNumber.Trim().ToUpperInvariant();
        Name = name.Trim();
        AssetCategoryId = assetCategoryId;
        AssetTypeId = assetTypeId;
        InternalCode = string.IsNullOrWhiteSpace(internalCode) ? null : internalCode.Trim().ToUpperInvariant();
        Description = description?.Trim();
        SerialNumber = string.IsNullOrWhiteSpace(serialNumber) ? null : serialNumber.Trim();
        Manufacturer = manufacturer?.Trim();
        Model = model?.Trim();
        OwnershipType = ownershipType;
        BranchId = branchId;
        LocationId = locationId;
        DepartmentId = departmentId;
        TeamId = teamId;
        PurchaseDate = purchaseDate;
        PurchaseCost = purchaseCost;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        SupplierName = supplierName?.Trim();
        PurchaseReference = purchaseReference?.Trim();
        WarrantyStartDate = warrantyStartDate;
        WarrantyEndDate = warrantyEndDate;
        WarrantyProvider = warrantyProvider?.Trim();
        WarrantyReference = warrantyReference?.Trim();
        CommissionedAtUtc = commissionedAtUtc;
        PrimaryImageObjectKey = primaryImageObjectKey?.Trim();
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }

    public void ChangeStatus(AssetStatus newStatus, Guid? updatedBy = null)
    {
        Status = newStatus;
        if (newStatus == AssetStatus.Retired || newStatus == AssetStatus.Disposed)
        {
            RetiredAtUtc ??= DateTime.UtcNow;
            IsActive = false;
        }
        else if (newStatus == AssetStatus.Inactive)
        {
            IsActive = false;
        }
        else
        {
            IsActive = true;
        }
        MarkUpdated(updatedBy);
    }

    public void SetCondition(AssetCondition condition, Guid? updatedBy = null)
    {
        Condition = condition;
        if (condition == AssetCondition.Damaged || condition == AssetCondition.Unsafe)
        {
            if (Status == AssetStatus.Available || Status == AssetStatus.Assigned)
            {
                Status = AssetStatus.Damaged;
            }
        }
        MarkUpdated(updatedBy);
    }

    public void AssignLocation(Guid? branchId, Guid? locationId, string? reason = null, Guid? userId = null)
    {
        BranchId = branchId;
        LocationId = locationId;
        MarkUpdated(userId);
    }

    public void SetCustodian(Guid? employeeId, Guid? updatedBy = null)
    {
        CurrentCustodianEmployeeId = employeeId;
        if (employeeId.HasValue && Status == AssetStatus.Available)
        {
            Status = AssetStatus.Assigned;
        }
        else if (!employeeId.HasValue && !CurrentAssignedVehicleId.HasValue && Status == AssetStatus.Assigned)
        {
            Status = AssetStatus.Available;
        }
        MarkUpdated(updatedBy);
    }

    public void SetAssignedVehicle(Guid? vehicleId, Guid? updatedBy = null)
    {
        CurrentAssignedVehicleId = vehicleId;
        if (vehicleId.HasValue && Status == AssetStatus.Available)
        {
            Status = AssetStatus.Assigned;
        }
        else if (!vehicleId.HasValue && !CurrentCustodianEmployeeId.HasValue && Status == AssetStatus.Assigned)
        {
            Status = AssetStatus.Available;
        }
        MarkUpdated(updatedBy);
    }

    public void SetActive(bool isActive, Guid? updatedBy = null)
    {
        IsActive = isActive;
        if (!isActive && Status == AssetStatus.Available)
        {
            Status = AssetStatus.Inactive;
        }
        else if (isActive && Status == AssetStatus.Inactive)
        {
            Status = AssetStatus.Available;
        }
        MarkUpdated(updatedBy);
    }
}
