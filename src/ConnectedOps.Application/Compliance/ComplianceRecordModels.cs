using ConnectedOps.Domain.Compliance;

namespace ConnectedOps.Application.Compliance;

public sealed record ComplianceRecordDto(
    Guid Id,
    Guid TenantId,
    Guid ComplianceRequirementId,
    string RequirementCode,
    string RequirementName,
    ComplianceRequirementType RequirementType,
    ComplianceSubjectType SubjectType,
    Guid? VehicleId,
    string? VehiclePlateOrVin,
    Guid? DriverId,
    string? DriverName,
    Guid? AssetId,
    string? AssetNumberOrName,
    string? ReferenceNumber,
    DateTime? IssueDateUtc,
    DateTime? EffectiveFromUtc,
    DateTime? ExpiryDateUtc,
    int? DaysRemaining,
    ComplianceStatus Status,
    DateTime? VerifiedAtUtc,
    Guid? VerifiedByUserId,
    string? Notes,
    DateTime CreatedAtUtc,
    IReadOnlyList<ComplianceDocumentDto>? Documents = null);

public sealed record ComplianceDocumentDto(
    Guid Id,
    Guid TenantId,
    Guid ComplianceRecordId,
    string DocumentType,
    string Title,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? IssueDateUtc,
    DateTime? ExpiryDateUtc,
    DateTime UploadedAtUtc,
    Guid? UploadedByUserId,
    string? Notes);

public sealed record CreateComplianceRecordRequest(
    Guid ComplianceRequirementId,
    ComplianceSubjectType SubjectType,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? AssetId = null,
    string? ReferenceNumber = null,
    DateTime? IssueDateUtc = null,
    DateTime? EffectiveFromUtc = null,
    DateTime? ExpiryDateUtc = null,
    ComplianceStatus Status = ComplianceStatus.Valid,
    string? Notes = null);

public sealed record UpdateComplianceRecordRequest(
    string? ReferenceNumber = null,
    DateTime? IssueDateUtc = null,
    DateTime? EffectiveFromUtc = null,
    DateTime? ExpiryDateUtc = null,
    ComplianceStatus Status = ComplianceStatus.Valid,
    string? Notes = null);

public sealed record VerifyComplianceRecordRequest(
    Guid? VerifiedByUserId = null,
    string? Notes = null);

public sealed record AddComplianceDocumentRequest(
    string DocumentType,
    string Title,
    string FileObjectKey,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    DateTime? IssueDateUtc = null,
    DateTime? ExpiryDateUtc = null,
    string? Notes = null);

public sealed record ComplianceRecordFilterRequest(
    ComplianceSubjectType? SubjectType = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    Guid? AssetId = null,
    Guid? ComplianceRequirementId = null,
    ComplianceStatus? Status = null,
    bool? ExpiringSoon = null,
    bool? Expired = null,
    int? ExpiringWithinDays = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50);
