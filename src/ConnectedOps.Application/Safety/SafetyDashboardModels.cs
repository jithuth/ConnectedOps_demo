using ConnectedOps.Domain.Safety;

namespace ConnectedOps.Application.Safety;

public sealed record SafetyDashboardDto(
    int OpenIncidents,
    int CriticalIncidents,
    int IncidentsThisMonth,
    int NearMissesThisMonth,
    int OpenViolations,
    int OpenCorrectiveActions,
    int OverdueCorrectiveActions,
    int SafetyScore,
    SafetyRiskLevel OverallRiskLevel,
    IReadOnlyList<IncidentsByTypeDto> IncidentsByType,
    IReadOnlyList<IncidentsBySeverityDto> IncidentsBySeverity,
    IReadOnlyList<IncidentsByBranchDto> IncidentsByBranch,
    IReadOnlyList<MonthlyIncidentTrendDto> MonthlyTrend,
    IReadOnlyList<SafetyIncidentDto> RecentIncidents,
    IReadOnlyList<SafetyViolationDto> RecentViolations,
    IReadOnlyList<TopRepeatedViolationDto> TopRepeatedViolationTypes);

public sealed record IncidentsByTypeDto(
    SafetyIncidentType IncidentType,
    string TypeName,
    int Count);

public sealed record IncidentsBySeverityDto(
    SafetyIncidentSeverity Severity,
    string SeverityName,
    int Count);

public sealed record IncidentsByBranchDto(
    Guid? BranchId,
    string BranchName,
    int IncidentCount,
    int ViolationCount);

public sealed record MonthlyIncidentTrendDto(
    string MonthLabel,
    int Year,
    int Month,
    int IncidentCount,
    int AccidentCount,
    int NearMissCount);

public sealed record TopRepeatedViolationDto(
    SafetyViolationType ViolationType,
    string TypeName,
    int Count);

public sealed record SafetyScoreDto(
    int Score,
    SafetyRiskLevel RiskLevel,
    int BaseScore,
    int Deductions,
    IReadOnlyList<SafetyScoreDeductionDto> Breakdown);

public sealed record SafetyScoreDeductionDto(
    string Category,
    int PointsDeducted,
    string Reason);
