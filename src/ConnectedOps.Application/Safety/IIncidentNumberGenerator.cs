namespace ConnectedOps.Application.Safety;

public interface IIncidentNumberGenerator
{
    Task<string> GenerateIncidentNumberAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
