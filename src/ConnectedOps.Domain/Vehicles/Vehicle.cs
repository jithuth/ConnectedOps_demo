using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Vehicles;

public sealed class Vehicle : BaseEntity
{
    private readonly List<VehicleRegistration> _registrations = [];
    private readonly List<VehicleOdometerEntry> _odometerEntries = [];
    private readonly List<VehicleDocument> _documents = [];
    private readonly List<VehicleNote> _notes = [];

    private Vehicle()
    {
    }

    public Vehicle(
        Guid tenantId,
        string vehicleNumber,
        Guid categoryId,
        Guid makeId,
        Guid modelId,
        string? displayName = null,
        string? internalCode = null,
        string? registrationNumber = null,
        string? vin = null,
        string? chassisNumber = null,
        string? engineNumber = null,
        int? modelYear = null,
        int? manufactureYear = null,
        FuelType fuelType = FuelType.Diesel,
        TransmissionType transmissionType = TransmissionType.Automatic,
        OwnershipType ownershipType = OwnershipType.CompanyOwned,
        Guid? branchId = null,
        Guid? locationId = null,
        decimal currentOdometer = 0m,
        OdometerUnit odometerUnit = OdometerUnit.Kilometers,
        VehicleStatus status = VehicleStatus.Active,
        string? color = null,
        int? numberOfSeats = null,
        decimal? grossVehicleWeight = null,
        decimal? payloadCapacity = null,
        DateOnly? purchaseDate = null,
        decimal? purchasePrice = null,
        string? currencyCode = "USD",
        DateOnly? inServiceDate = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (categoryId == Guid.Empty)
            throw new ArgumentException("VehicleCategoryId is required.", nameof(categoryId));
        if (makeId == Guid.Empty)
            throw new ArgumentException("VehicleMakeId is required.", nameof(makeId));
        if (modelId == Guid.Empty)
            throw new ArgumentException("VehicleModelId is required.", nameof(modelId));
        if (currentOdometer < 0)
            throw new ArgumentException("Current odometer cannot be negative.", nameof(currentOdometer));

        TenantId = tenantId;
        SetVehicleNumber(vehicleNumber);
        VehicleCategoryId = categoryId;
        VehicleMakeId = makeId;
        VehicleModelId = modelId;

        DisplayName = string.IsNullOrWhiteSpace(displayName) ? VehicleNumber : displayName.Trim();
        InternalCode = NormalizeIdentifier(internalCode);
        RegistrationNumber = NormalizeIdentifier(registrationNumber);
        VIN = NormalizeIdentifier(vin);
        ChassisNumber = NormalizeIdentifier(chassisNumber);
        EngineNumber = NormalizeIdentifier(engineNumber);

        ModelYear = modelYear;
        ManufactureYear = manufactureYear;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        OwnershipType = ownershipType;

        BranchId = branchId;
        LocationId = locationId;

        CurrentOdometer = currentOdometer;
        OdometerUnit = odometerUnit;

        Status = status;
        Color = color?.Trim();
        NumberOfSeats = numberOfSeats;
        GrossVehicleWeight = grossVehicleWeight;
        PayloadCapacity = payloadCapacity;

        PurchaseDate = purchaseDate;
        PurchasePrice = purchasePrice;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();

        InServiceDate = inServiceDate ?? (status == VehicleStatus.InService ? DateOnly.FromDateTime(DateTime.UtcNow) : null);
        Notes = notes?.Trim();
        IsActive = status != VehicleStatus.Inactive && status != VehicleStatus.Retired && status != VehicleStatus.Sold && status != VehicleStatus.Scrapped;
    }

    public Guid TenantId { get; private set; }

    // Identifiers
    public string VehicleNumber { get; private set; } = string.Empty;
    public string? InternalCode { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? VIN { get; private set; }
    public string? ChassisNumber { get; private set; }
    public string? EngineNumber { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    // Classification
    public Guid VehicleCategoryId { get; private set; }
    public VehicleCategory VehicleCategory { get; private set; } = null!;
    public Guid VehicleMakeId { get; private set; }
    public VehicleMake VehicleMake { get; private set; } = null!;
    public Guid VehicleModelId { get; private set; }
    public VehicleModel VehicleModel { get; private set; } = null!;

    public int? ModelYear { get; private set; }
    public int? ManufactureYear { get; private set; }

    // Engineering & Capacity
    public FuelType FuelType { get; private set; }
    public TransmissionType TransmissionType { get; private set; }
    public string? Color { get; private set; }
    public int? NumberOfSeats { get; private set; }
    public decimal? GrossVehicleWeight { get; private set; }
    public decimal? PayloadCapacity { get; private set; }

    // Ownership
    public OwnershipType OwnershipType { get; private set; }
    public string? OwnerName { get; private set; }
    public string? LeaseCompany { get; private set; }
    public DateOnly? LeaseStartDate { get; private set; }
    public DateOnly? LeaseEndDate { get; private set; }
    public decimal? MonthlyLeaseCost { get; private set; }
    public string? CurrencyCode { get; private set; } = "USD";
    public DateOnly? PurchaseDate { get; private set; }
    public decimal? PurchasePrice { get; private set; }

    // Organization Assignment
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }

    // Odometer
    public decimal CurrentOdometer { get; private set; }
    public OdometerUnit OdometerUnit { get; private set; }

    // Lifecycle
    public VehicleStatus Status { get; private set; }
    public DateOnly? InServiceDate { get; private set; }
    public DateOnly? OutOfServiceDate { get; private set; }
    public bool IsActive { get; private set; }

    // Media & Notes
    public string? PrimaryImageObjectKey { get; private set; }
    public string? Notes { get; private set; }

    // Relationships
    public VehicleSpecification? Specification { get; private set; }
    public IReadOnlyCollection<VehicleRegistration> Registrations => _registrations;
    public IReadOnlyCollection<VehicleOdometerEntry> OdometerEntries => _odometerEntries;
    public IReadOnlyCollection<VehicleDocument> Documents => _documents;
    public IReadOnlyCollection<VehicleNote> NotesList => _notes;

    public void UpdateGeneralInfo(
        string vehicleNumber,
        string? displayName,
        string? color,
        int? numberOfSeats,
        string? notes)
    {
        SetVehicleNumber(vehicleNumber);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? VehicleNumber : displayName.Trim();
        Color = color?.Trim();
        NumberOfSeats = numberOfSeats;
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void UpdateIdentification(
        string? internalCode,
        string? registrationNumber,
        string? vin,
        string? chassisNumber,
        string? engineNumber)
    {
        InternalCode = NormalizeIdentifier(internalCode);
        RegistrationNumber = NormalizeIdentifier(registrationNumber);
        VIN = NormalizeIdentifier(vin);
        ChassisNumber = NormalizeIdentifier(chassisNumber);
        EngineNumber = NormalizeIdentifier(engineNumber);
        MarkUpdated();
    }

    public void UpdateClassification(
        Guid categoryId,
        Guid makeId,
        Guid modelId,
        int? modelYear,
        int? manufactureYear,
        FuelType fuelType,
        TransmissionType transmissionType)
    {
        if (categoryId == Guid.Empty) throw new ArgumentException("VehicleCategoryId is required.", nameof(categoryId));
        if (makeId == Guid.Empty) throw new ArgumentException("VehicleMakeId is required.", nameof(makeId));
        if (modelId == Guid.Empty) throw new ArgumentException("VehicleModelId is required.", nameof(modelId));

        VehicleCategoryId = categoryId;
        VehicleMakeId = makeId;
        VehicleModelId = modelId;
        ModelYear = modelYear;
        ManufactureYear = manufactureYear;
        FuelType = fuelType;
        TransmissionType = transmissionType;
        MarkUpdated();
    }

    public void UpdateOwnership(
        OwnershipType ownershipType,
        string? ownerName,
        string? leaseCompany,
        DateOnly? leaseStartDate,
        DateOnly? leaseEndDate,
        decimal? monthlyLeaseCost,
        DateOnly? purchaseDate,
        decimal? purchasePrice,
        string? currencyCode)
    {
        if (leaseStartDate.HasValue && leaseEndDate.HasValue && leaseEndDate < leaseStartDate)
            throw new ArgumentException("Lease end date cannot be earlier than lease start date.");

        OwnershipType = ownershipType;
        OwnerName = ownerName?.Trim();
        LeaseCompany = leaseCompany?.Trim();
        LeaseStartDate = leaseStartDate;
        LeaseEndDate = leaseEndDate;
        MonthlyLeaseCost = monthlyLeaseCost;
        PurchaseDate = purchaseDate;
        PurchasePrice = purchasePrice;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void AssignBranch(Guid? branchId)
    {
        BranchId = branchId;
        MarkUpdated();
    }

    public void AssignLocation(Guid? locationId)
    {
        LocationId = locationId;
        MarkUpdated();
    }

    public void UpdateOdometer(decimal newReading)
    {
        if (newReading < CurrentOdometer)
            throw new InvalidOperationException($"New odometer reading ({newReading}) cannot be lower than current reading ({CurrentOdometer}).");

        CurrentOdometer = newReading;
        MarkUpdated();
    }

    public void SetStatus(VehicleStatus newStatus)
    {
        if (Status == newStatus)
            return;

        // Terminal states check
        if (Status is VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped)
        {
            throw new InvalidOperationException($"Vehicle in terminal status '{Status}' cannot transition to '{newStatus}'.");
        }

        // Validate transitions
        var isValid = (Status, newStatus) switch
        {
            (VehicleStatus.Draft, VehicleStatus.Active) => true,
            (VehicleStatus.Draft, VehicleStatus.Inactive) => true,
            (VehicleStatus.Active, VehicleStatus.InService) => true,
            (VehicleStatus.Active, VehicleStatus.OutOfService) => true,
            (VehicleStatus.Active, VehicleStatus.UnderMaintenance) => true,
            (VehicleStatus.Active, VehicleStatus.Reserved) => true,
            (VehicleStatus.Active, VehicleStatus.Inactive) => true,
            (VehicleStatus.Active, VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped) => true,

            (VehicleStatus.InService, VehicleStatus.OutOfService) => true,
            (VehicleStatus.InService, VehicleStatus.UnderMaintenance) => true,
            (VehicleStatus.InService, VehicleStatus.Active) => true,
            (VehicleStatus.InService, VehicleStatus.Reserved) => true,
            (VehicleStatus.InService, VehicleStatus.Inactive) => true,
            (VehicleStatus.InService, VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped) => true,

            (VehicleStatus.OutOfService, VehicleStatus.InService) => true,
            (VehicleStatus.OutOfService, VehicleStatus.UnderMaintenance) => true,
            (VehicleStatus.OutOfService, VehicleStatus.Active) => true,
            (VehicleStatus.OutOfService, VehicleStatus.Inactive) => true,
            (VehicleStatus.OutOfService, VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped) => true,

            (VehicleStatus.UnderMaintenance, VehicleStatus.InService) => true,
            (VehicleStatus.UnderMaintenance, VehicleStatus.OutOfService) => true,
            (VehicleStatus.UnderMaintenance, VehicleStatus.Active) => true,
            (VehicleStatus.UnderMaintenance, VehicleStatus.Inactive) => true,
            (VehicleStatus.UnderMaintenance, VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped) => true,

            (VehicleStatus.Reserved, VehicleStatus.InService) => true,
            (VehicleStatus.Reserved, VehicleStatus.Active) => true,
            (VehicleStatus.Reserved, VehicleStatus.OutOfService) => true,
            (VehicleStatus.Reserved, VehicleStatus.Inactive) => true,

            (VehicleStatus.Inactive, VehicleStatus.Active) => true,
            (VehicleStatus.Inactive, VehicleStatus.InService) => true,
            (VehicleStatus.Inactive, VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped) => true,

            _ => false
        };

        if (!isValid)
        {
            throw new InvalidOperationException($"Invalid status transition from '{Status}' to '{newStatus}'.");
        }

        Status = newStatus;
        if (newStatus == VehicleStatus.InService && !InServiceDate.HasValue)
        {
            InServiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }
        else if (newStatus is VehicleStatus.OutOfService or VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped)
        {
            OutOfServiceDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        IsActive = newStatus != VehicleStatus.Inactive && newStatus != VehicleStatus.Retired && newStatus != VehicleStatus.Sold && newStatus != VehicleStatus.Scrapped;
        MarkUpdated();
    }

    public void Activate()
    {
        if (Status is VehicleStatus.Retired or VehicleStatus.Sold or VehicleStatus.Scrapped)
            throw new InvalidOperationException($"Cannot activate a vehicle in terminal status '{Status}'.");

        Status = VehicleStatus.Active;
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        Status = VehicleStatus.Inactive;
        IsActive = false;
        MarkUpdated();
    }

    public void SetPrimaryImage(string? objectKey)
    {
        PrimaryImageObjectKey = objectKey?.Trim();
        MarkUpdated();
    }

    private void SetVehicleNumber(string vehicleNumber)
    {
        if (string.IsNullOrWhiteSpace(vehicleNumber))
            throw new ArgumentException("VehicleNumber is required.", nameof(vehicleNumber));

        VehicleNumber = vehicleNumber.Trim().ToUpperInvariant();
    }

    private static string? NormalizeIdentifier(string? id)
    {
        return string.IsNullOrWhiteSpace(id) ? null : id.Trim().ToUpperInvariant();
    }
}
