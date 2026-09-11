namespace ConnectedOps.Application.Telematics;

public interface ITrackingProviderService
{
    Task<IReadOnlyCollection<TrackingProviderDto>> GetProvidersAsync(
        CancellationToken cancellationToken = default);

    Task<TrackingProviderDto?> GetProviderByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TrackingProviderDto?> GetProviderByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<TrackingProviderDto> CreateProviderAsync(
        CreateTrackingProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TrackingDeviceTypeDto>> GetDeviceTypesAsync(
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceTypeDto?> GetDeviceTypeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceTypeDto?> GetDeviceTypeByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceTypeDto> CreateDeviceTypeAsync(
        CreateTrackingDeviceTypeRequest request,
        CancellationToken cancellationToken = default);
}
