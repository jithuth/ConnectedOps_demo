namespace ConnectedOps.Application.Security;

public interface ISecurityLogService
{
    Task WriteAsync(
        CreateSecurityLogRequest request,
        CancellationToken cancellationToken = default);

    Task<SecurityLogPage> GetAsync(
        SecurityLogQuery query,
        CancellationToken cancellationToken = default);
}