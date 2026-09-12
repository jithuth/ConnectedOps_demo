using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public sealed record ComplianceEvaluationResultDto(
    Guid SubjectId,
    ComplianceSubjectType SubjectType,
    string SubjectIdentifier,
    string SubjectDisplayName,
    Guid? BranchId,
    string? BranchName,
    ComplianceStatus OverallStatus,
    int Score,
    ComplianceScoreGrade Grade,
    int TotalRequirements,
    int ValidCount,
    int ExpiringCount,
    int ExpiredCount,
    int MissingCount,
    int PendingVerificationCount,
    int RejectedCount,
    int ExceptionCount,
    DateTime EvaluatedAtUtc,
    IReadOnlyList<ComplianceRequirementItemResultDto> RequirementResults);

public sealed record ComplianceRequirementItemResultDto(
    Guid ComplianceRequirementId,
    string RequirementCode,
    string RequirementName,
    ComplianceRequirementType RequirementType,
    ComplianceValidityType ValidityType,
    bool IsMandatory,
    ComplianceStatus Status,
    DateTime? IssueDateUtc,
    DateTime? ExpiryDateUtc,
    int? DaysRemaining,
    string? ReferenceNumber,
    Guid? AuthoritativeRecordId,
    string? AuthoritativeSourceType,
    bool HasActiveException,
    string? Notes);

public sealed record SubjectComplianceSummaryDto(
    Guid SubjectId,
    ComplianceSubjectType SubjectType,
    string SubjectIdentifier,
    string SubjectDisplayName,
    Guid? BranchId,
    string? BranchName,
    ComplianceStatus OverallStatus,
    int Score,
    ComplianceScoreGrade Grade,
    int ValidCount,
    int ExpiringCount,
    int ExpiredCount,
    int MissingCount,
    DateTime CalculatedAtUtc);

public sealed record ComplianceScoreDto(
    int Score,
    ComplianceScoreGrade Grade,
    int BaseScore,
    int TotalPenalties,
    IReadOnlyList<ComplianceScorePenaltyDto> Penalties);

public sealed record ComplianceScorePenaltyDto(
    string Reason,
    int PointsDeducted,
    string RequirementCode,
    string RequirementName);
