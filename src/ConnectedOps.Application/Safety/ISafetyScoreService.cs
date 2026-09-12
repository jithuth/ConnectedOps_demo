using ConnectedOps.Domain.Safety;

namespace ConnectedOps.Application.Safety;

public interface ISafetyScoreService
{
    Task<SafetyScoreDto> CalculateTenantSafetyScoreAsync(CancellationToken cancellationToken = default);
    SafetyRiskLevel MapScoreToRiskLevel(int score);
}
