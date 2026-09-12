using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public sealed record ComplianceExceptionDto(
    Guid Id,
    Guid TenantId,
    Guid ComplianceRequirementId,
    string RequirementCode,
    string RequirementName,
    ComplianceSubjectType SubjectType,
    Guid? VehicleId,
    string? VehiclePlateOrVin,
    Guid? DriverId,
    string? DriverName,
    Guid? AssetId,
    string? AssetNumberOrName,
    string Reason,
    Guid ApprovedByUserId,
    string? ApprovedByUserName,
    DateTime EffectiveFromUtc,
    DateTime EffectiveToUtc,
    ComplianceExceptionStatus Status,
    bool IsActiveNow,
    DateTime CreatedAtUtc);

public sealed record CreateComplianceExceptionRequest(
    Guid ComplianceRequirementId,
    ComplianceSubjectType SubjectType,
    string Reason,
    DateTime EffectiveFromUtc,
    DateTime EffectiveToUtc,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? AssetId = null,
    ComplianceExceptionStatus Status = ComplianceExceptionStatus.Approved);

public sealed record ApproveComplianceExceptionRequest(
    string? Notes = null);

public sealed record RejectComplianceExceptionRequest(
    string? Reason = null);

public sealed record ComplianceExceptionFilterRequest(
    ComplianceSubjectType? SubjectType = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? AssetId = null,
    Guid? ComplianceRequirementId = null,
    ComplianceExceptionStatus? Status = null,
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 50);
