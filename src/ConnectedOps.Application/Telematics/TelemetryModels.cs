using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public sealed record NormalizedTelemetryMessage(
    string DeviceIdentifier,
    string Provider,
    DateTime RecordedAtUtc,
    DateTime ReceivedAtUtc,
    double? Latitude = null,
    double? Longitude = null,
    double? AltitudeMeters = null,
    decimal? SpeedKph = null,
    decimal? HeadingDegrees = null,
    bool? IgnitionOn = null,
    decimal? OdometerKm = null,
    decimal? EngineHours = null,
    decimal? FuelLevelPercent = null,
    decimal? FuelVolumeLiters = null,
    decimal? BatteryVoltage = null,
    decimal? ExternalPowerVoltage = null,
    int? GsmSignal = null,
    int? GpsSatellites = null,
    decimal? TemperatureCelsius = null,
    bool? DigitalInput1 = null,
    bool? DigitalInput2 = null,
    int? RawEventCode = null,
    TelemetryEventType EventType = TelemetryEventType.Periodic,
    string? ProviderMessageId = null,
    IReadOnlyDictionary<int, object>? AdditionalIoElements = null);

public sealed record TelemetryIngestionResult(
    bool Success,
    Guid? TelemetryRecordId,
    string DeviceIdentifier,
    Guid? DeviceId,
    Guid? VehicleId,
    bool IsDuplicate = false,
    bool IsQuarantined = false,
    string? ErrorMessage = null,
    DateTime? RecordedAtUtc = null);

public sealed record TelemetryRecordDto(
    Guid Id,
    Guid TenantId,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    Guid? VehicleId,
    string? VehicleDisplayName,
    DateTime RecordedAtUtc,
    DateTime ReceivedAtUtc,
    double? Latitude,
    double? Longitude,
    double? AltitudeMeters,
    decimal? SpeedKph,
    decimal? HeadingDegrees,
    bool? IgnitionOn,
    decimal? OdometerKm,
    decimal? EngineHours,
    decimal? FuelLevelPercent,
    decimal? FuelVolumeLiters,
    decimal? BatteryVoltage,
    decimal? ExternalPowerVoltage,
    int? GsmSignal,
    int? GpsSatellites,
    decimal? TemperatureCelsius,
    bool? DigitalInput1,
    bool? DigitalInput2,
    int? RawEventCode,
    TelemetryEventType EventType,
    string SourceProvider,
    string? ProviderMessageId,
    DateTime CreatedAtUtc);

public sealed record VehicleTelemetryStateDto(
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    Guid TenantId,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    DateTime RecordedAtUtc,
    DateTime ReceivedAtUtc,
    double Latitude,
    double Longitude,
    double? AltitudeMeters,
    decimal? SpeedKph,
    decimal? HeadingDegrees,
    bool? IgnitionOn,
    decimal? OdometerKm,
    decimal? EngineHours,
    decimal? FuelLevelPercent,
    decimal? BatteryVoltage,
    decimal? ExternalPowerVoltage,
    int? SignalStrength,
    DeviceConnectivityStatus ConnectivityStatus,
    DateTime LastUpdatedAtUtc);

public sealed record LiveVehicleTrackingDto(
    Guid VehicleId,
    string VehicleNumber,
    string RegistrationNumber,
    string VehicleName,
    Guid DeviceId,
    string DeviceIdentifier,
    string? DeviceName,
    double Latitude,
    double Longitude,
    decimal? SpeedKph,
    decimal? HeadingDegrees,
    bool? IgnitionOn,
    DateTime RecordedAtUtc,
    DeviceConnectivityStatus ConnectivityStatus,
    Guid? DriverId,
    string? DriverName,
    Guid? BranchId,
    string? BranchName,
    int? BatteryLevelPercent,
    int? SignalStrength,
    decimal? OdometerKm);

public sealed record LiveTrackingQueryParameters(
    Guid? BranchId = null,
    int? VehicleStatus = null,
    DeviceConnectivityStatus? ConnectivityStatus = null,
    bool? IgnitionOn = null,
    bool? IsMoving = null,
    string? Search = null);

public sealed record TelemetryHistoryQueryParameters(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    TelemetryEventType? EventType = null,
    int Page = 1,
    int PageSize = 50);

public sealed record TelemetryHistoryPagedResult(
    IReadOnlyCollection<TelemetryRecordDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record TelemetryIngestionFailureDto(
    Guid Id,
    Guid? TenantId,
    string DeviceIdentifier,
    string Provider,
    DateTime ReceivedAtUtc,
    string Reason,
    string TraceId,
    string? PayloadReference,
    DateTime CreatedAtUtc);
