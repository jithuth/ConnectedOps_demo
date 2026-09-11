using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public sealed record DeviceHealthDto(
    Guid DeviceId,
    string DeviceIdentifier,
    string? IMEI,
    string? Name,
    string ProviderCode,
    string DeviceTypeName,
    Guid? VehicleId,
    string? VehicleDisplayName,
    string? VehicleRegistrationNumber,
    DateTime? LastSeenAtUtc,
    DateTime? LastTelemetryAtUtc,
    DeviceConnectivityStatus ConnectivityStatus,
    DeviceHealthState HealthState,
    int? BatteryLevelPercent,
    decimal? ExternalPowerVoltage,
    decimal? BatteryVoltage,
    int? SignalStrength,
    string? FirmwareVersion);

public sealed record DeviceHealthQueryParameters(
    string? Search = null,
    Guid? ProviderId = null,
    DeviceHealthState? HealthState = null,
    DeviceConnectivityStatus? ConnectivityStatus = null,
    bool? HasVehicle = null,
    int Page = 1,
    int PageSize = 25);
