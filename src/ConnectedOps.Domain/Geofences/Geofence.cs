using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Geofences;

public sealed class Geofence : BaseEntity
{
    private Geofence()
    {
    }

    public Geofence(
        Guid tenantId,
        string name,
        string code,
        GeofenceType geofenceType,
        string? description = null,
        double? centerLatitude = null,
        double? centerLongitude = null,
        double? radiusMeters = null,
        string? polygonGeoJson = null,
        Guid? branchId = null,
        Guid? locationId = null,
        string colorHex = "#3B82F6",
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Geofence name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Geofence code is required.", nameof(code));

        TenantId = tenantId;
        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        GeofenceType = geofenceType;
        Description = description?.Trim();
        CenterLatitude = centerLatitude;
        CenterLongitude = centerLongitude;
        RadiusMeters = radiusMeters;
        PolygonGeoJson = polygonGeoJson?.Trim();
        BranchId = branchId;
        LocationId = locationId;
        ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#3B82F6" : colorHex.Trim();
        IsActive = isActive;
        IsDeleted = false;
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string? Description { get; private set; }
    public GeofenceType GeofenceType { get; private set; }

    public double? CenterLatitude { get; private set; }
    public double? CenterLongitude { get; private set; }
    public double? RadiusMeters { get; private set; }
    public string? PolygonGeoJson { get; private set; }

    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public Guid? LocationId { get; private set; }
    public Location? Location { get; private set; }

    public string ColorHex { get; private set; } = "#3B82F6";
    public bool IsActive { get; private set; } = true;

    public void Update(
        string name,
        GeofenceType geofenceType,
        string? description,
        double? centerLatitude,
        double? centerLongitude,
        double? radiusMeters,
        string? polygonGeoJson,
        Guid? branchId,
        Guid? locationId,
        string colorHex,
        Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Geofence name is required.", nameof(name));

        Name = name.Trim();
        GeofenceType = geofenceType;
        Description = description?.Trim();
        CenterLatitude = centerLatitude;
        CenterLongitude = centerLongitude;
        RadiusMeters = radiusMeters;
        PolygonGeoJson = polygonGeoJson?.Trim();
        BranchId = branchId;
        LocationId = locationId;
        ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#3B82F6" : colorHex.Trim();

        MarkUpdated(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        MarkUpdated(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        MarkUpdated(userId);
    }

    public void MarkDeleted(Guid? userId = null)
    {
        IsDeleted = true;
        IsActive = false;
        MarkUpdated(userId);
    }
}
