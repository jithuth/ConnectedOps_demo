using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.ColdChain;

public interface IColdChainService
{
    Task<ColdChainDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<CargoSensorDeviceDto>> GetSensorsPagedAsync(
        SensorFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ColdChainExcursionDto>> GetExcursionsPagedAsync(
        ExcursionFilterRequest request,
        CancellationToken cancellationToken = default);

    Task<CargoSensorDeviceDto> RegisterSensorAsync(
        RegisterCargoSensorRequest request,
        CancellationToken cancellationToken = default);

    Task<CargoTelemetryReadingDto> RecordTelemetryAsync(
        RecordCargoTelemetryRequest request,
        CancellationToken cancellationToken = default);

    Task<ColdChainExcursionDto> ResolveExcursionAsync(
        Guid excursionId,
        ResolveExcursionRequest request,
        CancellationToken cancellationToken = default);
}
