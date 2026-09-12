using ConnectedOps.Domain.Safety;

namespace ConnectedOps.Application.Safety;

public sealed record CorrectiveActionDto(
    Guid Id,
    Guid TenantId,
    string Title,
    string Description,
    CorrectiveActionPriority Priority,
    DateTime? DueDateUtc,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    Guid? SafetyIncidentId,
    string? IncidentNumber,
    Guid? SafetyViolationId,
    string? ViolationDescription,
    Guid? ComplianceRecordId,
    string? ComplianceRequirementName,
    CorrectiveActionStatus Status,
    bool IsOverdue,
    DateTime? CompletedAtUtc,
    Guid? CompletedByUserId,
    string? CompletedByUserName,
    bool VerificationRequired,
    DateTime? VerifiedAtUtc,
    Guid? VerifiedByUserId,
    string? VerifiedByUserName,
    string? ResolutionNotes,
    DateTime CreatedAtUtc);

public sealed record CreateCorrectiveActionRequest(
    string Title,
    string Description,
    CorrectiveActionPriority Priority = CorrectiveActionPriority.Medium,
    DateTime? DueDateUtc = null,
    Guid? AssignedEmployeeId = null,
    Guid? SafetyIncidentId = null,
    Guid? SafetyViolationId = null,
    Guid? ComplianceRecordId = null,
    bool VerificationRequired = false);

public sealed record UpdateCorrectiveActionRequest(
    string Title,
    string Description,
    CorrectiveActionPriority Priority,
    DateTime? DueDateUtc = null,
    Guid? AssignedEmployeeId = null,
    bool VerificationRequired = false);

public sealed record CompleteCorrectiveActionRequest(
    string? ResolutionNotes = null);

public sealed record VerifyCorrectiveActionRequest(
    string? VerificationNotes = null);

public sealed record CorrectiveActionFilterRequest(
    CorrectiveActionStatus? Status = null,
    CorrectiveActionPriority? Priority = null,
    Guid? AssignedEmployeeId = null,
    Guid? SafetyIncidentId = null,
    Guid? SafetyViolationId = null,
    Guid? ComplianceRecordId = null,
    bool? OverdueOnly = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50);
