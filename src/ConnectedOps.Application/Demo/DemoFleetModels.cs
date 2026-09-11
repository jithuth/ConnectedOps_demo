namespace ConnectedOps.Application.Demo;

public sealed record DemoFleetStatusDto(
    bool IsRunning,
    bool IsEnabled,
    int TotalDemoVehicles,
    int TotalRoutes,
    int UpdateIntervalSeconds,
    DateTime? LastTickAtUtc,
    long TotalTicksExecuted,
    string StatusMessage);

public sealed record DemoVehicleStateDto(
    Guid VehicleId,
    string VehicleNumber,
    string DisplayName,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    Guid RouteId,
    string RouteName,
    int CurrentPointIndex,
    int TotalRoutePoints,
    double Latitude,
    double Longitude,
    decimal SpeedKph,
    decimal HeadingDegrees,
    bool IgnitionOn,
    decimal OdometerKm,
    int BatteryPercentage,
    int GsmSignal,
    DateTime LastUpdateUtc,
    bool IsSimulatingOffline);

public sealed record CreateDemoFleetRequest(
    int VehicleCount = 10,
    double? CenterLatitude = null,
    double? CenterLongitude = null);
