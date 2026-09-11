using ConnectedOps.Domain.Common;

namespace ConnectedOps.Application.Telematics;

public interface ITrackingDeviceService
{
    Task<IReadOnlyCollection<TrackingDeviceListItemDto>> GetDevicesPagedAsync(
        TrackingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<int> GetDeviceCountAsync(
        TrackingDeviceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceDto?> GetDeviceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceDto?> GetDeviceByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceDto?> GetDeviceByImeiAsync(
        string imei,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceDto> CreateDeviceAsync(
        CreateTrackingDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceDto> UpdateDeviceAsync(
        Guid id,
        UpdateTrackingDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDeviceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TrackingDeviceListItemDto>> GetAvailableDevicesForAssignmentAsync(
        CancellationToken cancellationToken = default);
}
