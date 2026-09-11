using ConnectedOps.Application.Auditing;

namespace ConnectedOps.Tests.Common;

public sealed class TestAuditLogService : IAuditLogService
{
    public List<CreateAuditLogRequest> WrittenLogs { get; } = [];

    public Task WriteAsync(CreateAuditLogRequest request, CancellationToken cancellationToken = default)
    {
        WrittenLogs.Add(request);
        return Task.CompletedTask;
    }

    public Task<AuditLogPage> GetAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuditLogPage([], 0, 1, 10));
    }
}
