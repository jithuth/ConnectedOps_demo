namespace ConnectedOps.Application.Telematics;

public interface IDeviceProvisioningService
{
    Task<DeviceProvisioningRecordDto> ProvisionDeviceAsync(
        Guid deviceId,
        ProvisionDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<DeviceProvisioningRecordDto> DeprovisionDeviceAsync(
        Guid deviceId,
        DeprovisionDeviceRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeviceProvisioningRecordDto>> GetProvisioningHistoryAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}
