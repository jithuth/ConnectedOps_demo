using ConnectedOps.Application.Compliance;
using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceScoreService : IComplianceScoreService
{
    public ComplianceScoreDto CalculateScore(IEnumerable<ComplianceRequirementItemResultDto> requirementResults)
    {
        const int baseScore = 100;
        var penalties = new List<ComplianceScorePenaltyDto>();
        var results = requirementResults.ToList();

        if (results.Count == 0)
        {
            return new ComplianceScoreDto(
                Score: 100,
                Grade: ComplianceScoreGrade.A,
                BaseScore: baseScore,
                TotalPenalties: 0,
                Penalties: penalties);
        }

        foreach (var req in results)
        {
            if (req.HasActiveException)
                continue;

            switch (req.Status)
            {
                case ComplianceStatus.Expired:
                    int expiredPenalty = req.IsMandatory ? 30 : 15;
                    penalties.Add(new ComplianceScorePenaltyDto(
                        Reason: req.IsMandatory ? "Expired mandatory requirement" : "Expired requirement",
                        PointsDeducted: expiredPenalty,
                        RequirementCode: req.RequirementCode,
                        RequirementName: req.RequirementName));
                    break;

                case ComplianceStatus.Missing:
                    int missingPenalty = req.IsMandatory ? 25 : 10;
                    penalties.Add(new ComplianceScorePenaltyDto(
                        Reason: req.IsMandatory ? "Missing mandatory requirement" : "Missing requirement",
                        PointsDeducted: missingPenalty,
                        RequirementCode: req.RequirementCode,
                        RequirementName: req.RequirementName));
                    break;

                case ComplianceStatus.Rejected:
                case ComplianceStatus.Suspended:
                    int rejectedPenalty = req.IsMandatory ? 25 : 10;
                    penalties.Add(new ComplianceScorePenaltyDto(
                        Reason: "Rejected or suspended compliance record",
                        PointsDeducted: rejectedPenalty,
                        RequirementCode: req.RequirementCode,
                        RequirementName: req.RequirementName));
                    break;

                case ComplianceStatus.ExpiringSoon:
                    penalties.Add(new ComplianceScorePenaltyDto(
                        Reason: "Expiring soon within reminder window",
                        PointsDeducted: 5,
                        RequirementCode: req.RequirementCode,
                        RequirementName: req.RequirementName));
                    break;

                case ComplianceStatus.PendingVerification:
                    penalties.Add(new ComplianceScorePenaltyDto(
                        Reason: "Pending verification",
                        PointsDeducted: 5,
                        RequirementCode: req.RequirementCode,
                        RequirementName: req.RequirementName));
                    break;
            }
        }

        int totalPenalties = penalties.Sum(p => p.PointsDeducted);
        int finalScore = Math.Max(0, baseScore - totalPenalties);
        var grade = MapScoreToGrade(finalScore);

        return new ComplianceScoreDto(
            Score: finalScore,
            Grade: grade,
            BaseScore: baseScore,
            TotalPenalties: totalPenalties,
            Penalties: penalties);
    }

    public ComplianceScoreGrade MapScoreToGrade(int score)
    {
        return score switch
        {
            >= 90 => ComplianceScoreGrade.A,
            >= 75 => ComplianceScoreGrade.B,
            >= 60 => ComplianceScoreGrade.C,
            >= 40 => ComplianceScoreGrade.D,
            _ => ComplianceScoreGrade.Critical
        };
    }
}
