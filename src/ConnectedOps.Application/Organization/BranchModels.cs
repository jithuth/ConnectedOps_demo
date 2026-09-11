using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Application.Organization;

public sealed record BranchListItemDto(
    Guid Id,
    string Name,
    string Code,
    BranchType Type,
    string TypeName,
    bool IsHeadOffice,
    bool IsActive,
    string? City,
    string? CountryCode,
    int LocationCount,
    int EmployeeCount);

public sealed record BranchDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Code,
    BranchType Type,
    string TypeName,
    bool IsHeadOffice,
    bool IsActive,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId,
    int LocationCount,
    int DepartmentCount,
    int EmployeeCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateBranchRequest(
    string Name,
    string Code,
    BranchType Type,
    bool IsHeadOffice,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId);

public sealed record UpdateBranchRequest(
    string Name,
    string Code,
    BranchType Type,
    bool IsHeadOffice,
    string? Email,
    string? Phone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrProvince,
    string? PostalCode,
    string? CountryCode,
    string? TimeZoneId);
