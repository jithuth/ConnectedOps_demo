namespace ConnectedOps.Application.Maps;

public interface IFleetMapService
{
    Task<IReadOnlyCollection<FleetMapVehicleDto>> GetFleetMapVehiclesAsync(
        FleetMapQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<GeoJsonFeatureCollection> GetFleetMapGeoJsonAsync(
        FleetMapQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<FleetMapVehicleDto?> GetVehicleMapStateAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VehicleTrailPointDto>> GetVehicleTrailAsync(
        Guid vehicleId,
        FleetMapTrailQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<FleetMapDashboardDto> GetMapDashboardMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<MapClientConfigurationDto> GetMapClientConfigurationAsync(
        CancellationToken cancellationToken = default);
}
