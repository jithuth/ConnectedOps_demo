using System.Text.Json.Serialization;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Domain.Vehicles;

namespace ConnectedOps.Application.Maps;

public enum VehicleMarkerState
{
    Moving = 1,
    Stopped = 2,
    Parked = 3,
    Offline = 4,
    Untracked = 5
}

public sealed record FleetMapVehicleDto(
    Guid VehicleId,
    string VehicleNumber,
    string? RegistrationNumber,
    string DisplayName,
    string? MakeModel,
    Guid? CategoryId,
    string? CategoryName,
    Guid? DriverId,
    string? DriverName,
    string? DriverPhone,
    Guid? TrackingDeviceId,
    string? DeviceIdentifier,
    double? Latitude,
    double? Longitude,
    decimal? SpeedKph,
    decimal? HeadingDegrees,
    bool? IgnitionOn,
    decimal? OdometerKm,
    decimal? EngineHours,
    decimal? BatteryVoltage,
    decimal? FuelLevelPercent,
    int? SignalStrength,
    DateTime? RecordedAtUtc,
    DateTime? ReceivedAtUtc,
    DeviceConnectivityStatus ConnectivityStatus,
    VehicleMarkerState MarkerState,
    VehicleStatus VehicleStatus,
    Guid? BranchId,
    string? BranchName,
    Guid? CurrentUsageSessionId,
    int GeofenceCount);

public sealed record VehicleTrailPointDto(
    DateTime RecordedAtUtc,
    double Latitude,
    double Longitude,
    decimal? SpeedKph,
    decimal? HeadingDegrees,
    bool? IgnitionOn,
    decimal? OdometerKm);

public sealed record FleetMapQueryParameters(
    string? Search = null,
    Guid? BranchId = null,
    Guid? CategoryId = null,
    VehicleStatus? Status = null,
    DeviceConnectivityStatus? ConnectivityStatus = null,
    VehicleMarkerState? MarkerState = null,
    bool? IgnitionOn = null,
    bool IncludeUntracked = false);

public sealed record FleetMapTrailQueryParameters(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int MaxPoints = 500);

public sealed record FleetMapDashboardDto(
    int TrackedVehicles,
    int OnlineVehicles,
    int OfflineVehicles,
    int MovingVehicles,
    int StoppedVehicles,
    int ParkedVehicles,
    int UntrackedVehicles,
    int VehiclesInsideGeofences,
    int GeofenceEntriesToday,
    int GeofenceExitsToday,
    int ActiveGeofences);

#region GeoJSON RFC 7946 Standard Models

public sealed class GeoJsonFeatureCollection
{
    [JsonPropertyName("type")]
    public string Type => "FeatureCollection";

    [JsonPropertyName("features")]
    public List<GeoJsonFeature> Features { get; set; } = [];
}

public sealed class GeoJsonFeature
{
    [JsonPropertyName("type")]
    public string Type => "Feature";

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("geometry")]
    public GeoJsonPointGeometry Geometry { get; set; } = null!;

    [JsonPropertyName("properties")]
    public Dictionary<string, object?> Properties { get; set; } = new();
}

public sealed class GeoJsonPointGeometry
{
    [JsonPropertyName("type")]
    public string Type => "Point";

    /// <summary>
    /// RFC 7946 GeoJSON specification requires [longitude, latitude].
    /// </summary>
    [JsonPropertyName("coordinates")]
    public double[] Coordinates { get; set; } = new double[2];

    public GeoJsonPointGeometry()
    {
    }

    public GeoJsonPointGeometry(double longitude, double latitude)
    {
        Coordinates = [longitude, latitude];
    }
}

#endregion
