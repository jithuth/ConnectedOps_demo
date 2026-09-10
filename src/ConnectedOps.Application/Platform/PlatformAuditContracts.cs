using ConnectedOps.Application.Auditing;

namespace ConnectedOps.Application.Platform;

public sealed record PlatformAuditLogQuery(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? TenantId = null,
    Guid? UserId = null,
    string? Action = null,
    string? EntityType = null,
    string? EntityId = null,
    int Page = 1,
    int PageSize = 20);

public interface IPlatformAuditService
{
    Task<AuditLogPage> GetAuditLogsAsync(
        PlatformAuditLogQuery query,
        CancellationToken cancellationToken = default);
}
