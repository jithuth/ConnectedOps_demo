using ConnectedOps.Application.Telematics;
using ConnectedOps.Domain.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class DeviceConnectivityService : IDeviceConnectivityService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public DeviceConnectivityService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DeviceConnectivityStatus EvaluateStatus(DateTime? lastSeenAtUtc, int offlineThresholdMinutes = 5)
    {
        if (!lastSeenAtUtc.HasValue)
        {
            return DeviceConnectivityStatus.Unknown;
        }

        var age = DateTime.UtcNow - lastSeenAtUtc.Value;
        if (age.TotalMinutes <= Math.Max(1, offlineThresholdMinutes))
        {
            return DeviceConnectivityStatus.Online;
        }

        return DeviceConnectivityStatus.Offline;
    }

    public async Task<DeviceConnectivityStatus> EvaluateStatusAsync(
        DateTime? lastSeenAtUtc,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!lastSeenAtUtc.HasValue)
        {
            return DeviceConnectivityStatus.Unknown;
        }

        var settings = await _dbContext.TelematicsSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

        int threshold = settings?.OfflineThresholdMinutes ?? 5;
        return EvaluateStatus(lastSeenAtUtc, threshold);
    }
}
