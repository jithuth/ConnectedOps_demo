using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Fuel;

public sealed class FuelStation : BaseEntity
{
    private FuelStation()
    {
    }

    public FuelStation(
        Guid tenantId,
        string code,
        string name,
        string? vendorName = null,
        FuelStationType stationType = FuelStationType.Internal,
        string? address = null,
        double? latitude = null,
        double? longitude = null,
        Guid? branchId = null,
        string? contactPhone = null,
        string? notes = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (latitude.HasValue && (latitude.Value < -90.0 || latitude.Value > 90.0))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees.", nameof(latitude));
        if (longitude.HasValue && (longitude.Value < -180.0 || longitude.Value > 180.0))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees.", nameof(longitude));

        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        VendorName = vendorName?.Trim();
        StationType = stationType;
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        BranchId = branchId;
        ContactPhone = contactPhone?.Trim();
        Notes = notes?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? VendorName { get; private set; }
    public FuelStationType StationType { get; private set; }
    public string? Address { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public string? ContactPhone { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string code,
        string name,
        string? vendorName,
        FuelStationType stationType,
        string? address,
        double? latitude,
        double? longitude,
        Guid? branchId,
        string? contactPhone,
        string? notes,
        bool isActive,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (latitude.HasValue && (latitude.Value < -90.0 || latitude.Value > 90.0))
            throw new ArgumentException("Latitude must be between -90 and 90 degrees.", nameof(latitude));
        if (longitude.HasValue && (longitude.Value < -180.0 || longitude.Value > 180.0))
            throw new ArgumentException("Longitude must be between -180 and 180 degrees.", nameof(longitude));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        VendorName = vendorName?.Trim();
        StationType = stationType;
        Address = address?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        BranchId = branchId;
        ContactPhone = contactPhone?.Trim();
        Notes = notes?.Trim();
        IsActive = isActive;
        MarkUpdated(updatedBy);
    }
}
