using ConnectedOps.Application.Telematics;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Telematics;

public sealed class TelemetryDeduplicationService : ITelemetryDeduplicationService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public TelemetryDeduplicationService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsDuplicateAsync(
        Guid tenantId,
        Guid trackingDeviceId,
        NormalizedTelemetryMessage message,
        CancellationToken cancellationToken = default)
    {
        // 1. If provider message ID is present, check by provider message ID
        if (!string.IsNullOrWhiteSpace(message.ProviderMessageId))
        {
            var existsByMsgId = await _dbContext.TelemetryRecords
                .AnyAsync(
                    x => x.TenantId == tenantId &&
                         x.TrackingDeviceId == trackingDeviceId &&
                         x.ProviderMessageId == message.ProviderMessageId,
                    cancellationToken);

            if (existsByMsgId)
            {
                return true;
            }
        }

        // 2. Check by exact timestamp and device id
        var existsByTimestamp = await _dbContext.TelemetryRecords
            .AnyAsync(
                x => x.TenantId == tenantId &&
                     x.TrackingDeviceId == trackingDeviceId &&
                     x.RecordedAtUtc == message.RecordedAtUtc,
                cancellationToken);

        return existsByTimestamp;
    }
}
