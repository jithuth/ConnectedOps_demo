using ConnectedOps.Domain.Ev;

namespace ConnectedOps.Application.Ev;

public sealed record ChargingStationDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    ChargingStationType StationType,
    ChargingConnectorType ConnectorType,
    decimal MaxPowerKw,
    int TotalPlugs,
    int AvailablePlugs,
    string? Address,
    double? Latitude,
    double? Longitude,
    decimal OffPeakRatePerKwh,
    decimal PeakRatePerKwh,
    string Currency,
    bool IsActive);

public sealed record VehicleBatteryStateDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleVin,
    string VehicleName,
    decimal BatteryCapacityKwh,
    decimal StateOfChargePercent,
    decimal StateOfHealthPercent,
    decimal RemainingRangeKm,
    decimal BatteryPackTempCelsius,
    EvChargingStatus ChargingStatus,
    decimal ActiveChargingPowerKw,
    int CycleCount,
    int TargetSocLimitPercent,
    bool IsOffPeakOnlyCharging,
    BatteryHealthCondition HealthCondition,
    DateTime LastTelemetryAtUtc);

public sealed record ChargingSessionDto(
    Guid Id,
    Guid TenantId,
    Guid VehicleId,
    string VehicleVin,
    Guid ChargingStationId,
    string StationName,
    DateTime StartedAtUtc,
    DateTime? CompletedAtUtc,
    decimal StartSocPercent,
    decimal? EndSocPercent,
    decimal EnergyDeliveredKwh,
    decimal TotalCost,
    decimal Co2SavedKg,
    string Currency,
    bool IsActive,
    bool IsScheduled,
    DateTime? ScheduledStartUtc);

public sealed record EvFleetDashboardDto(
    int TotalEvCount,
    int ActiveChargingCount,
    decimal AverageSocPercent,
    decimal AverageSohPercent,
    decimal TotalEnergyDeliveredKwh,
    decimal TotalCo2SavedKg,
    decimal TotalChargingCost,
    IReadOnlyList<VehicleBatteryStateDto> Vehicles,
    IReadOnlyList<ChargingStationDto> Stations,
    IReadOnlyList<ChargingSessionDto> ActiveSessions);

public sealed record CreateChargingStationRequest(
    string Code,
    string Name,
    ChargingStationType StationType,
    ChargingConnectorType ConnectorType,
    decimal MaxPowerKw,
    int TotalPlugs,
    string? Address = null,
    double? Latitude = null,
    double? Longitude = null,
    decimal OffPeakRatePerKwh = 0.15m,
    decimal PeakRatePerKwh = 0.35m,
    string Currency = "USD");

public sealed record UpdateVehicleBatteryTelemetryRequest(
    Guid VehicleId,
    decimal StateOfChargePercent,
    decimal RemainingRangeKm,
    decimal BatteryPackTempCelsius,
    EvChargingStatus ChargingStatus,
    decimal ActiveChargingPowerKw);

public sealed record ConfigureSmartChargingRequest(
    Guid VehicleId,
    int TargetSocLimitPercent,
    bool IsOffPeakOnlyCharging);

public sealed record StartChargingSessionRequest(
    Guid VehicleId,
    Guid ChargingStationId,
    decimal StartSocPercent,
    bool IsScheduled = false,
    DateTime? ScheduledStartUtc = null);

public sealed record CompleteChargingSessionRequest(
    Guid SessionId,
    decimal EndSocPercent,
    decimal EnergyDeliveredKwh,
    decimal TotalCost);

public sealed record EvFilterRequest(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    EvChargingStatus? Status = null);
