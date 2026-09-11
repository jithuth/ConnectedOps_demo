using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Drivers;

public sealed record DriverListItemDto(
    Guid Id,
    string DriverNumber,
    string DisplayName,
    string FirstName,
    string LastName,
    string Phone,
    string? Email,
    DriverType DriverType,
    string DriverTypeName,
    DriverStatus Status,
    string StatusName,
    DriverAvailabilityStatus AvailabilityStatus,
    string AvailabilityStatusName,
    Guid? BranchId,
    string? BranchName,
    Guid? DepartmentId,
    string? DepartmentName,
    string? PrimaryLicenseNumber,
    DateOnly? PrimaryLicenseExpiryDate,
    bool IsLicenseExpired,
    bool IsLicenseExpiringSoon,
    Guid? CurrentVehicleId,
    string? CurrentVehicleNumber,
    string? CurrentVehicleDisplayName,
    bool IsActive,
    DateTime CreatedAtUtc,
    int DocumentCount,
    int ExpiringDocumentCount,
    int CertificationCount,
    int ExpiringCertificationCount);

public sealed record DriverDetailDto(
    Guid Id,
    Guid TenantId,
    string DriverNumber,
    Guid? EmployeeId,
    string? EmployeeNumber,
    string? EmployeeName,
    string FirstName,
    string? MiddleName,
    string LastName,
    string DisplayName,
    string? Email,
    string Phone,
    string? AlternatePhone,
    DriverType DriverType,
    string DriverTypeName,
    DriverStatus Status,
    string StatusName,
    DriverAvailabilityStatus AvailabilityStatus,
    string AvailabilityStatusName,
    Guid? BranchId,
    string? BranchName,
    Guid? DepartmentId,
    string? DepartmentName,
    DateOnly? HireDate,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? DateOfBirth,
    string? NationalityCode,
    string? PrimaryLicenseNumber,
    string? PreferredLanguage,
    string? ProfileImageObjectKey,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DriverVehicleAssignmentDto? CurrentVehicleAssignment,
    IReadOnlyCollection<DriverLicenseDto> Licenses,
    IReadOnlyCollection<DriverCertificationDto> Certifications,
    IReadOnlyCollection<DriverDocumentDto> Documents,
    IReadOnlyCollection<DriverVehicleAssignmentDto> RecentAssignments,
    IReadOnlyCollection<DriverEmergencyContactDto> EmergencyContacts,
    IReadOnlyCollection<DriverNoteDto> NotesList);

public sealed record CreateDriverRequest(
    string DriverNumber,
    string FirstName,
    string LastName,
    string Phone,
    DriverType DriverType,
    string? MiddleName = null,
    string? DisplayName = null,
    string? Email = null,
    string? AlternatePhone = null,
    DriverStatus Status = DriverStatus.Active,
    Guid? EmployeeId = null,
    Guid? BranchId = null,
    Guid? DepartmentId = null,
    DateOnly? HireDate = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    DateOnly? DateOfBirth = null,
    string? NationalityCode = null,
    string? PreferredLanguage = null,
    string? Notes = null,
    string? InitialLicenseNumber = null,
    string? InitialLicenseCountryCode = null,
    DateOnly? InitialLicenseExpiryDate = null,
    string? InitialLicenseAuthority = null,
    string? InitialLicenseCategoryCode = null);

public sealed record UpdateDriverRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? DisplayName,
    string Phone,
    string? AlternatePhone,
    string? Email,
    DriverType DriverType,
    DateOnly? DateOfBirth,
    string? NationalityCode,
    string? PreferredLanguage,
    DateOnly? HireDate,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? Notes);

public sealed record ChangeDriverStatusRequest(
    DriverStatus NewStatus,
    string? Notes);

public sealed record LinkDriverEmployeeRequest(
    Guid? EmployeeId);

public sealed record AssignDriverBranchRequest(
    Guid? BranchId);

public sealed record AssignDriverDepartmentRequest(
    Guid? DepartmentId);

public sealed record CreateDriverEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? AlternatePhone,
    bool IsPrimary);

public sealed record UpdateDriverEmergencyContactRequest(
    string Name,
    string Relationship,
    string Phone,
    string? AlternatePhone,
    bool IsPrimary);

public sealed record DriverEmergencyContactDto(
    Guid Id,
    Guid DriverId,
    string Name,
    string Relationship,
    string Phone,
    string? AlternatePhone,
    bool IsPrimary);

public sealed record CreateDriverNoteRequest(
    string NoteText);

public sealed record DriverNoteDto(
    Guid Id,
    Guid DriverId,
    string NoteText,
    Guid? CreatedByUserId,
    string? CreatedByUserName,
    DateTime CreatedAtUtc);

public sealed class DriverQueryParameters
{
    public string? SearchTerm { get; set; }
    public DriverStatus? Status { get; set; }
    public DriverType? DriverType { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public bool? HasActiveAssignment { get; set; }
    public bool? IsLicenseExpired { get; set; }
    public bool? IsLicenseExpiringSoon { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
