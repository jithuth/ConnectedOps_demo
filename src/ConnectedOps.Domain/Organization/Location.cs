using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class Location : BaseEntity
{
    private Location()
    {
    }

    public Location(
        Guid tenantId,
        Guid branchId,
        string name,
        string code,
        LocationType type = LocationType.Depot,
        double? latitude = null,
        double? longitude = null,
        double? geofenceRadiusMeters = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? stateOrProvince = null,
        string? postalCode = null,
        string? countryCode = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (branchId == Guid.Empty)
            throw new ArgumentException("BranchId is required.", nameof(branchId));

        TenantId = tenantId;
        BranchId = branchId;
        SetName(name);
        SetCode(code);
        Type = type;
        IsActive = true;

        SetCoordinates(latitude, longitude, geofenceRadiusMeters);
        UpdateAddress(addressLine1, addressLine2, city, stateOrProvince, postalCode, countryCode);
    }

    public Guid TenantId { get; private set; }
    public Guid BranchId { get; private set; }
    public Branch Branch { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public LocationType Type { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public double? GeofenceRadiusMeters { get; private set; }

    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? StateOrProvince { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public bool IsActive { get; private set; }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Location name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Location name cannot exceed 200 characters.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Location code is required.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new ArgumentException("Location code cannot exceed 50 characters.", nameof(code));

        Code = normalized;
        MarkUpdated();
    }

    public void Update(
        Guid branchId,
        string name,
        string code,
        LocationType type,
        double? latitude,
        double? longitude,
        double? geofenceRadiusMeters,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? countryCode)
    {
        if (branchId == Guid.Empty)
            throw new ArgumentException("BranchId is required.", nameof(branchId));

        BranchId = branchId;
        SetName(name);
        SetCode(code);
        Type = type;

        SetCoordinates(latitude, longitude, geofenceRadiusMeters);
        UpdateAddress(addressLine1, addressLine2, city, stateOrProvince, postalCode, countryCode);
    }

    public void SetCoordinates(double? latitude, double? longitude, double? geofenceRadiusMeters)
    {
        if (latitude.HasValue && (latitude < -90 || latitude > 90))
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");

        if (longitude.HasValue && (longitude < -180 || longitude > 180))
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");

        if (geofenceRadiusMeters.HasValue && geofenceRadiusMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(geofenceRadiusMeters), "Geofence radius must be positive.");

        Latitude = latitude;
        Longitude = longitude;
        GeofenceRadiusMeters = geofenceRadiusMeters;

        MarkUpdated();
    }

    public void UpdateAddress(
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? countryCode)
    {
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        StateOrProvince = stateOrProvince?.Trim();
        PostalCode = postalCode?.Trim();
        CountryCode = countryCode?.Trim()?.ToUpperInvariant();

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
}
