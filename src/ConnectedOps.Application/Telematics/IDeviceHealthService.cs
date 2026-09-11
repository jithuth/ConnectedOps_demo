namespace ConnectedOps.Application.Telematics;

public interface IDeviceHealthService
{
    Task<DeviceHealthDto?> GetDeviceHealthByIdAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeviceHealthDto>> GetDeviceHealthPagedAsync(
        DeviceHealthQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<int> GetDeviceHealthCountAsync(
        DeviceHealthQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
