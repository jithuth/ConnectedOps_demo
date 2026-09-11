using ConnectedOps.Domain.Telematics;

namespace ConnectedOps.Application.Telematics;

public interface IDeviceConnectivityService
{
    DeviceConnectivityStatus EvaluateStatus(DateTime? lastSeenAtUtc, int offlineThresholdMinutes = 5);

    Task<DeviceConnectivityStatus> EvaluateStatusAsync(
        DateTime? lastSeenAtUtc,
        Guid tenantId,
        CancellationToken cancellationToken = default);
}
