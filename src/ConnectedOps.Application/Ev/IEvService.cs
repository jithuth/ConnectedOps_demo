using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Ev;

public interface IEvService
{
    Task<EvFleetDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<VehicleBatteryStateDto>> GetVehicleBatteriesPagedAsync(
        EvFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChargingStationDto>> GetStationsAsync(CancellationToken cancellationToken = default);

    Task<ChargingStationDto> CreateStationAsync(
        CreateChargingStationRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleBatteryStateDto> UpdateTelemetryAsync(
        UpdateVehicleBatteryTelemetryRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleBatteryStateDto> ConfigureSmartChargingAsync(
        ConfigureSmartChargingRequest request,
        CancellationToken cancellationToken = default);

    Task<ChargingSessionDto> StartSessionAsync(
        StartChargingSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<ChargingSessionDto> CompleteSessionAsync(
        CompleteChargingSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ChargingSessionDto>> GetSessionsPagedAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
