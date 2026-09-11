using ConnectedOps.Domain.Geofences;

namespace ConnectedOps.Application.Geofences;

public sealed record GeofenceDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    string? Description,
    GeofenceType GeofenceType,
    double? CenterLatitude,
    double? CenterLongitude,
    double? RadiusMeters,
    string? PolygonGeoJson,
    Guid? BranchId,
    string? BranchName,
    Guid? LocationId,
    string? LocationName,
    string ColorHex,
    bool IsActive,
    int ActiveVehiclesInsideCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record GeofenceListItemDto(
    Guid Id,
    string Name,
    string Code,
    GeofenceType GeofenceType,
    double? CenterLatitude,
    double? CenterLongitude,
    double? RadiusMeters,
    string? PolygonGeoJson,
    Guid? BranchId,
    string? BranchName,
    string ColorHex,
    bool IsActive,
    int ActiveVehiclesInsideCount);

public sealed record CreateGeofenceRequest(
    string Name,
    string Code,
    GeofenceType GeofenceType,
    string? Description = null,
    double? CenterLatitude = null,
    double? CenterLongitude = null,
    double? RadiusMeters = null,
    string? PolygonGeoJson = null,
    Guid? BranchId = null,
    Guid? LocationId = null,
    string ColorHex = "#3B82F6");

public sealed record UpdateGeofenceRequest(
    string Name,
    GeofenceType GeofenceType,
    string? Description = null,
    double? CenterLatitude = null,
    double? CenterLongitude = null,
    double? RadiusMeters = null,
    string? PolygonGeoJson = null,
    Guid? BranchId = null,
    Guid? LocationId = null,
    string ColorHex = "#3B82F6");

public sealed record GeofenceQueryParameters(
    string? Search = null,
    GeofenceType? GeofenceType = null,
    Guid? BranchId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 25);

public sealed record GeofenceVehiclePresenceDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string DisplayName,
    Guid? DriverId,
    string? DriverName,
    DateTime LastEnteredAtUtc,
    DateTime LastEvaluatedAtUtc,
    double? Latitude,
    double? Longitude,
    decimal? SpeedKph,
    bool? IgnitionOn);

public sealed record VehicleGeofenceMembershipDto(
    Guid GeofenceId,
    string GeofenceName,
    string GeofenceCode,
    GeofenceType GeofenceType,
    string ColorHex,
    DateTime LastEnteredAtUtc);

public sealed record GeofenceEventDto(
    Guid Id,
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    Guid GeofenceId,
    string GeofenceName,
    string GeofenceCode,
    GeofenceEventType EventType,
    DateTime OccurredAtUtc,
    DateTime ReceivedAtUtc,
    double Latitude,
    double Longitude,
    Guid? DriverId,
    string? DriverName,
    Guid? TrackingDeviceId,
    Guid? TelemetryRecordId);

public sealed record GeofenceEventQueryParameters(
    Guid? GeofenceId = null,
    Guid? VehicleId = null,
    GeofenceEventType? EventType = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 25);

public sealed record GeofenceEventPage(
    IReadOnlyList<GeofenceEventDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
