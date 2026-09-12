using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public interface IComplianceScoreService
{
    ComplianceScoreDto CalculateScore(IEnumerable<ComplianceRequirementItemResultDto> requirementResults);
    ComplianceScoreGrade MapScoreToGrade(int score);
}
