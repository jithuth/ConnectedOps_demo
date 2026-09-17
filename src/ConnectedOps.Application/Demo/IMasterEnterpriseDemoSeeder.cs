namespace ConnectedOps.Application.Demo;

public sealed record EnterpriseDemoSeedingResultDto(
    Guid TenantId,
    string TenantName,
    int VehiclesCreated,
    int EvStationsCreated,
    int ChargingSessionsCreated,
    int VrpRunsCreated,
    int SubsystemsEvaluated,
    int PredictiveAlertsCreated,
    bool BrandingConfigured,
    string CustomDomainRegistered,
    string AuditPackageNumber,
    string Message);

public interface IMasterEnterpriseDemoSeeder
{
    Task<EnterpriseDemoSeedingResultDto> SeedEnterpriseDemoDataAsync(
        Guid? targetTenantId = null,
        CancellationToken cancellationToken = default);
}
