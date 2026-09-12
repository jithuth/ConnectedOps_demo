using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public sealed record ComplianceDashboardDto(
    int TotalVehicles,
    int CompliantVehicles,
    int NonCompliantVehicles,
    double VehicleComplianceRate,
    int TotalDrivers,
    int CompliantDrivers,
    int NonCompliantDrivers,
    double DriverComplianceRate,
    int TotalAssets,
    int CompliantAssets,
    int NonCompliantAssets,
    double AssetComplianceRate,
    int OverallScore,
    ComplianceScoreGrade OverallGrade,
    int ExpiringIn7Days,
    int ExpiringIn30Days,
    int ExpiredItems,
    int MissingMandatoryItems,
    int PendingVerificationItems,
    int ActiveExceptions,
    IReadOnlyList<ComplianceSubjectStatusCountDto> ComplianceBySubjectType,
    IReadOnlyList<ComplianceByBranchDto> ComplianceByBranch,
    IReadOnlyList<ComplianceExpiringItemDto> RecentlyExpired,
    IReadOnlyList<ComplianceExpiringItemDto> UpcomingExpiries,
    IReadOnlyList<SubjectComplianceSummaryDto> LowestComplianceSubjects);

public sealed record ComplianceSubjectStatusCountDto(
    ComplianceSubjectType SubjectType,
    int TotalCount,
    int CompliantCount,
    int ExpiringCount,
    int ExpiredCount,
    int MissingCount);

public sealed record ComplianceByBranchDto(
    Guid? BranchId,
    string BranchName,
    int TotalSubjects,
    int CompliantSubjects,
    int NonCompliantSubjects,
    double ComplianceRate);

public sealed record ComplianceExpiringItemDto(
    Guid SubjectId,
    ComplianceSubjectType SubjectType,
    string SubjectIdentifier,
    string SubjectDisplayName,
    Guid ComplianceRequirementId,
    string RequirementCode,
    string RequirementName,
    DateTime? ExpiryDateUtc,
    int? DaysRemaining,
    ComplianceStatus Status);
