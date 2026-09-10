namespace ConnectedOps.Application.Auditing;

public interface IAuditLogService
{
    Task WriteAsync(
        CreateAuditLogRequest request,
        CancellationToken cancellationToken = default);

    Task<AuditLogPage> GetAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken = default);
}