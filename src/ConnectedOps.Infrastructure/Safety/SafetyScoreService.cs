using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Safety;
using ConnectedOps.Domain.Safety;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Safety;

public sealed class SafetyScoreService : ISafetyScoreService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public SafetyScoreService(ConnectedOpsDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<SafetyScoreDto> CalculateTenantSafetyScoreAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        const int baseScore = 100;
        var deductions = new List<SafetyScoreDeductionDto>();

        // Open incidents
        var openIncidents = await _dbContext.SafetyIncidents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != SafetyIncidentStatus.Closed && x.Status != SafetyIncidentStatus.Cancelled)
            .ToListAsync(cancellationToken);

        int criticalIncidents = openIncidents.Count(i => i.Severity == SafetyIncidentSeverity.Critical);
        if (criticalIncidents > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Critical Incidents", criticalIncidents * 25, $"{criticalIncidents} open critical incident(s) (-25 pts each)"));
        }

        int highIncidents = openIncidents.Count(i => i.Severity == SafetyIncidentSeverity.High);
        if (highIncidents > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("High Severity Incidents", highIncidents * 15, $"{highIncidents} open high severity incident(s) (-15 pts each)"));
        }

        int moderateIncidents = openIncidents.Count(i => i.Severity == SafetyIncidentSeverity.Moderate);
        if (moderateIncidents > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Moderate Severity Incidents", moderateIncidents * 8, $"{moderateIncidents} open moderate severity incident(s) (-8 pts each)"));
        }

        int lowIncidents = openIncidents.Count(i => i.Severity == SafetyIncidentSeverity.Low);
        if (lowIncidents > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Low Severity Incidents", lowIncidents * 3, $"{lowIncidents} open low severity incident(s) (-3 pts each)"));
        }

        // Unresolved violations in last 90 days
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var unresolvedViolations = await _dbContext.SafetyViolations
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsResolved && x.OccurredAtUtc >= cutoff)
            .ToListAsync(cancellationToken);

        int critViolations = unresolvedViolations.Count(v => v.Severity == SafetyIncidentSeverity.Critical);
        if (critViolations > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Critical Violations", critViolations * 15, $"{critViolations} unresolved critical violation(s) (-15 pts each)"));
        }

        int highViolations = unresolvedViolations.Count(v => v.Severity == SafetyIncidentSeverity.High);
        if (highViolations > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("High Severity Violations", highViolations * 10, $"{highViolations} unresolved high violation(s) (-10 pts each)"));
        }

        int modViolations = unresolvedViolations.Count(v => v.Severity == SafetyIncidentSeverity.Moderate);
        if (modViolations > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Moderate Violations", modViolations * 5, $"{modViolations} unresolved moderate violation(s) (-5 pts each)"));
        }

        int lowViolations = unresolvedViolations.Count(v => v.Severity == SafetyIncidentSeverity.Low);
        if (lowViolations > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Low Violations", lowViolations * 2, $"{lowViolations} unresolved low violation(s) (-2 pts each)"));
        }

        // Overdue corrective actions
        var now = DateTime.UtcNow;
        var overdueActions = await _dbContext.CorrectiveActions
            .AsNoTracking()
            .CountAsync(x => x.TenantId == tenantId && x.DueDateUtc.HasValue && x.DueDateUtc.Value < now &&
                             (x.Status == CorrectiveActionStatus.Open || x.Status == CorrectiveActionStatus.InProgress || x.Status == CorrectiveActionStatus.Overdue),
                        cancellationToken);

        if (overdueActions > 0)
        {
            deductions.Add(new SafetyScoreDeductionDto("Overdue Corrective Actions", overdueActions * 10, $"{overdueActions} overdue corrective action(s) (-10 pts each)"));
        }

        int totalDeductions = deductions.Sum(d => d.PointsDeducted);
        int finalScore = Math.Max(0, baseScore - totalDeductions);
        var riskLevel = MapScoreToRiskLevel(finalScore);

        return new SafetyScoreDto(finalScore, riskLevel, baseScore, totalDeductions, deductions);
    }

    public SafetyRiskLevel MapScoreToRiskLevel(int score)
    {
        return score switch
        {
            >= 85 => SafetyRiskLevel.Low,
            >= 70 => SafetyRiskLevel.Medium,
            >= 50 => SafetyRiskLevel.High,
            _ => SafetyRiskLevel.Critical
        };
    }
}
