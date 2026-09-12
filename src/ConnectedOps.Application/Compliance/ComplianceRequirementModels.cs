using ConnectedOps.Domain.Compliance;
using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Compliance;

public sealed record ComplianceRequirementDto(
    Guid Id,
    Guid TenantId,
    string Code,
    string Name,
    string? Description,
    ComplianceSubjectType AppliesTo,
    ComplianceRequirementType RequirementType,
    ComplianceValidityType ValidityType,
    int? DefaultValidityDays,
    int DefaultReminderDays,
    bool IsMandatory,
    bool IsActive,
    int RuleCount,
    DateTime CreatedAtUtc,
    IReadOnlyList<ComplianceRequirementRuleDto>? Rules = null);

public sealed record ComplianceRequirementRuleDto(
    Guid Id,
    Guid TenantId,
    Guid ComplianceRequirementId,
    Guid? VehicleCategoryId,
    string? VehicleCategoryName,
    Guid? AssetCategoryId,
    string? AssetCategoryName,
    DriverType? DriverType,
    string? CountryCode,
    Guid? BranchId,
    string? BranchName,
    bool IsActive,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateComplianceRequirementRequest(
    string Code,
    string Name,
    ComplianceSubjectType AppliesTo,
    ComplianceRequirementType RequirementType,
    ComplianceValidityType ValidityType = ComplianceValidityType.Recurring,
    string? Description = null,
    int? DefaultValidityDays = null,
    int? DefaultReminderDays = 30,
    bool IsMandatory = true,
    bool IsActive = true);

public sealed record UpdateComplianceRequirementRequest(
    string Name,
    ComplianceSubjectType AppliesTo,
    ComplianceRequirementType RequirementType,
    ComplianceValidityType ValidityType,
    string? Description = null,
    int? DefaultValidityDays = null,
    int DefaultReminderDays = 30,
    bool IsMandatory = true,
    bool IsActive = true);

public sealed record CreateComplianceRequirementRuleRequest(
    Guid ComplianceRequirementId,
    Guid? VehicleCategoryId = null,
    Guid? AssetCategoryId = null,
    DriverType? DriverType = null,
    string? CountryCode = null,
    Guid? BranchId = null,
    bool IsActive = true,
    string? Notes = null);

public sealed record UpdateComplianceRequirementRuleRequest(
    Guid? VehicleCategoryId = null,
    Guid? AssetCategoryId = null,
    DriverType? DriverType = null,
    string? CountryCode = null,
    Guid? BranchId = null,
    bool IsActive = true,
    string? Notes = null);

public sealed record ComplianceRequirementFilterRequest(
    string? SearchTerm = null,
    ComplianceSubjectType? AppliesTo = null,
    ComplianceRequirementType? RequirementType = null,
    bool? IsActive = null,
    bool? IsMandatory = null,
    int Page = 1,
    int PageSize = 50);
