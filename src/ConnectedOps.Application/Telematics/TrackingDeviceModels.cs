using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public sealed record TrackingDeviceDto(
    Guid Id,
    Guid TenantId,
    string DeviceIdentifier,
    string? IMEI,
    string? SerialNumber,
    string? Name,
    Guid ProviderId,
    string ProviderName,
    string ProviderCode,
    Guid DeviceTypeId,
    string DeviceTypeName,
    string DeviceTypeCode,
    string? Model,
    string? Manufacturer,
    string? FirmwareVersion,
    string? SIMNumber,
    string? SIMICCID,
    string? PhoneNumber,
    TrackingDeviceStatus Status,
    DateTime? LastSeenAtUtc,
    DateTime? LastTelemetryAtUtc,
    double? LastKnownLatitude,
    double? LastKnownLongitude,
    decimal? LastKnownSpeedKph,
    decimal? LastKnownHeadingDegrees,
    bool? LastKnownIgnition,
    int? BatteryLevelPercent,
    decimal? ExternalPowerVoltage,
    decimal? BatteryVoltage,
    int? SignalStrength,
    bool IsActive,
    Guid? CurrentVehicleId,
    string? CurrentVehicleNumber,
    string? CurrentVehicleDisplayName,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record TrackingDeviceListItemDto(
    Guid Id,
    string DeviceIdentifier,
    string? IMEI,
    string? Name,
    string ProviderCode,
    string DeviceTypeName,
    TrackingDeviceStatus Status,
    DeviceConnectivityStatus ConnectivityStatus,
    DateTime? LastSeenAtUtc,
    Guid? CurrentVehicleId,
    string? CurrentVehicleDisplayName,
    int? BatteryLevelPercent,
    int? SignalStrength,
    bool IsActive);

public sealed record CreateTrackingDeviceRequest(
    string DeviceIdentifier,
    Guid ProviderId,
    Guid DeviceTypeId,
    string? IMEI = null,
    string? SerialNumber = null,
    string? Name = null,
    string? Model = null,
    string? Manufacturer = null,
    string? FirmwareVersion = null,
    string? SIMNumber = null,
    string? SIMICCID = null,
    string? PhoneNumber = null);

public sealed record UpdateTrackingDeviceRequest(
    string? Name,
    Guid ProviderId,
    Guid DeviceTypeId,
    string? IMEI = null,
    string? SerialNumber = null,
    string? Model = null,
    string? Manufacturer = null,
    string? FirmwareVersion = null,
    string? SIMNumber = null,
    string? SIMICCID = null,
    string? PhoneNumber = null);

public sealed record TrackingDeviceQueryParameters(
    string? Search = null,
    Guid? ProviderId = null,
    Guid? DeviceTypeId = null,
    TrackingDeviceStatus? Status = null,
    bool? IsAssigned = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 25);

public sealed record TrackingProviderDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Code,
    ProviderType ProviderType,
    string? Description,
    bool IsActive);

public sealed record CreateTrackingProviderRequest(
    string Name,
    string Code,
    ProviderType ProviderType,
    string? Description = null);

public sealed record TrackingDeviceTypeDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Code,
    string? Description,
    bool SupportsGps,
    bool SupportsIgnition,
    bool SupportsCanBus,
    bool SupportsObd,
    bool SupportsBattery,
    bool SupportsTemperature,
    bool SupportsFuel,
    bool SupportsBle,
    bool SupportsCommands,
    bool IsActive);

public sealed record CreateTrackingDeviceTypeRequest(
    string Name,
    string Code,
    string? Description = null,
    bool SupportsGps = true,
    bool SupportsIgnition = true,
    bool SupportsCanBus = false,
    bool SupportsObd = false,
    bool SupportsBattery = true,
    bool SupportsTemperature = false,
    bool SupportsFuel = false,
    bool SupportsBle = false,
    bool SupportsCommands = false);

public sealed record DeviceProvisioningRecordDto(
    Guid Id,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    ProvisioningStatus ProvisioningStatus,
    DateTime? ProvisionedAtUtc,
    Guid? ProvisionedByUserId,
    string? ProviderReference,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record ProvisionDeviceRequest(
    string? ProviderReference = null,
    string? Notes = null);

public sealed record DeprovisionDeviceRequest(
    string? Notes = null);

public sealed record TrackingDeviceVehicleAssignmentDto(
    Guid Id,
    Guid TenantId,
    Guid TrackingDeviceId,
    string DeviceIdentifier,
    Guid VehicleId,
    string VehicleNumber,
    string VehicleDisplayName,
    DateTime AssignedFromUtc,
    DateTime? AssignedToUtc,
    bool IsPrimary,
    bool IsActive,
    Guid? AssignedByUserId,
    Guid? EndedByUserId,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record AssignDeviceToVehicleRequest(
    Guid TrackingDeviceId,
    Guid VehicleId,
    bool IsPrimary = true,
    DateTime? AssignedFromUtc = null,
    string? Notes = null);

public sealed record EndDeviceAssignmentRequest(
    DateTime? EndedAtUtc = null,
    string? Notes = null);
