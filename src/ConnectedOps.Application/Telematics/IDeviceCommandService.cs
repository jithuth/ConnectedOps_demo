namespace ConnectedOps.Application.Telematics;

public interface IDeviceCommandService
{
    Task<DeviceCommandDto> SendCommandAsync(
        Guid deviceId,
        CreateDeviceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<DeviceCommandDto> CancelCommandAsync(
        Guid commandId,
        CancelDeviceCommandRequest request,
        CancellationToken cancellationToken = default);

    Task<DeviceCommandDto?> GetCommandByIdAsync(
        Guid commandId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DeviceCommandDto>> GetCommandsForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);
}
