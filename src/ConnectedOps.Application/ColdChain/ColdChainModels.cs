using ConnectedOps.Domain.ColdChain;

namespace ConnectedOps.Application.ColdChain;

public sealed record SensorFilterRequest(
    Guid? VehicleId = null,
    bool? IsActive = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record ExcursionFilterRequest(
    ExcursionStatus? Status = null,
    ExcursionSeverity? Severity = null,
    Guid? VehicleId = null,
    int PageNumber = 1,
    int PageSize = 20);

public sealed record CargoSensorDeviceDto(
    Guid Id,
    string SensorTagNumber,
    string CompartmentName,
    Guid VehicleId,
    string VehiclePlateNumber,
    double MinTargetTemperatureCelsius,
    double MaxTargetTemperatureCelsius,
    int BatteryLevelPercent,
    string? MacAddress,
    bool IsActive,
    double? CurrentTemperatureCelsius,
    double? CurrentHumidityPercent,
    bool CurrentDoorOpen,
    DateTime LastReadingAtUtc);

public sealed record CargoTelemetryReadingDto(
    Guid Id,
    Guid CargoSensorDeviceId,
    string SensorTagNumber,
    DateTime RecordedAtUtc,
    double TemperatureCelsius,
    double? HumidityPercent,
    bool DoorOpen,
    ReeferMode ReeferMode,
    string ReeferModeName,
    double? SetpointTemperatureCelsius,
    double? Latitude,
    double? Longitude);

public sealed record ColdChainExcursionDto(
    Guid Id,
    Guid CargoSensorDeviceId,
    string SensorTagNumber,
    Guid VehicleId,
    string VehiclePlateNumber,
    string CompartmentName,
    double BreachTemperatureCelsius,
    double AllowableMinCelsius,
    double AllowableMaxCelsius,
    ExcursionSeverity Severity,
    string SeverityName,
    ExcursionStatus Status,
    string StatusName,
    DateTime StartedAtUtc,
    DateTime? ResolvedAtUtc,
    int? DurationMinutes,
    string? LocationName,
    string? ActionTaken);

public sealed record RegisterCargoSensorRequest(
    string SensorTagNumber,
    string CompartmentName,
    Guid VehicleId,
    double MinTargetTemperatureCelsius,
    double MaxTargetTemperatureCelsius,
    int BatteryLevelPercent = 100,
    string? MacAddress = null);

public sealed record RecordCargoTelemetryRequest(
    Guid CargoSensorDeviceId,
    DateTime RecordedAtUtc,
    double TemperatureCelsius,
    double? HumidityPercent = null,
    bool DoorOpen = false,
    ReeferMode ReeferMode = ReeferMode.Cooling,
    double? SetpointTemperatureCelsius = null,
    double? Latitude = null,
    double? Longitude = null);

public sealed record ResolveExcursionRequest(
    string ActionTaken);

public sealed record ColdChainDashboardDto(
    int TotalActiveSensors,
    int CompartmentsInToleranceCount,
    int ActiveExcursionsCount,
    int CriticalExcursionsCount,
    List<CargoSensorDeviceDto> ActiveSensors,
    List<ColdChainExcursionDto> RecentExcursions);
