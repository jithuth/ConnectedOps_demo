using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Organization;

namespace ConnectedOps.Domain.Drivers;

public sealed class Driver : BaseEntity
{
    private readonly List<DriverLicense> _licenses = [];
    private readonly List<DriverCertification> _certifications = [];
    private readonly List<DriverDocument> _documents = [];
    private readonly List<DriverVehicleAssignment> _vehicleAssignments = [];
    private readonly List<DriverEmergencyContact> _emergencyContacts = [];
    private readonly List<DriverNote> _notesList = [];

    private Driver()
    {
    }

    public Driver(
        Guid tenantId,
        string driverNumber,
        string firstName,
        string lastName,
        string phone,
        DriverType driverType = DriverType.Employee,
        string? middleName = null,
        string? displayName = null,
        string? email = null,
        string? alternatePhone = null,
        DriverStatus status = DriverStatus.Active,
        Guid? employeeId = null,
        Guid? branchId = null,
        Guid? departmentId = null,
        DateOnly? hireDate = null,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        DateOnly? dateOfBirth = null,
        string? nationalityCode = null,
        string? primaryLicenseNumber = null,
        string? preferredLanguage = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(driverNumber))
            throw new ArgumentException("Driver number is required.", nameof(driverNumber));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number is required.", nameof(phone));

        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date cannot be earlier than start date.");

        TenantId = tenantId;
        DriverNumber = driverNumber.Trim().ToUpperInvariant();
        FirstName = firstName.Trim();
        MiddleName = middleName?.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{FirstName} {LastName}".Trim()
            : displayName.Trim();
        Phone = phone.Trim();
        AlternatePhone = alternatePhone?.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        DriverType = driverType;
        Status = status;
        EmployeeId = employeeId;
        BranchId = branchId;
        DepartmentId = departmentId;
        HireDate = hireDate;
        StartDate = startDate;
        EndDate = endDate;
        DateOfBirth = dateOfBirth;
        NationalityCode = nationalityCode?.Trim().ToUpperInvariant();
        PrimaryLicenseNumber = primaryLicenseNumber?.Trim().ToUpperInvariant();
        PreferredLanguage = preferredLanguage?.Trim();
        Notes = notes?.Trim();
        IsActive = status is DriverStatus.Active or DriverStatus.Draft or DriverStatus.OnLeave;
    }

    public Guid TenantId { get; private set; }
    public string DriverNumber { get; private set; } = string.Empty;
    public Guid? EmployeeId { get; private set; }
    public Employee? Employee { get; private set; }

    public string FirstName { get; private set; } = string.Empty;
    public string? MiddleName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public string? AlternatePhone { get; private set; }

    public DriverType DriverType { get; private set; }
    public DriverStatus Status { get; private set; }

    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }

    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }

    public DateOnly? HireDate { get; private set; }
    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? NationalityCode { get; private set; }
    public string? PrimaryLicenseNumber { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public string? ProfileImageObjectKey { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation Collections
    public IReadOnlyCollection<DriverLicense> Licenses => _licenses;
    public IReadOnlyCollection<DriverCertification> Certifications => _certifications;
    public IReadOnlyCollection<DriverDocument> Documents => _documents;
    public IReadOnlyCollection<DriverVehicleAssignment> VehicleAssignments => _vehicleAssignments;
    public IReadOnlyCollection<DriverEmergencyContact> EmergencyContacts => _emergencyContacts;
    public IReadOnlyCollection<DriverNote> NotesList => _notesList;

    public void UpdateGeneralInfo(
        string firstName,
        string? middleName,
        string lastName,
        string? displayName,
        string? email,
        string phone,
        string? alternatePhone,
        DriverType driverType,
        DateOnly? dateOfBirth,
        string? nationalityCode,
        string? preferredLanguage,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number is required.", nameof(phone));

        FirstName = firstName.Trim();
        MiddleName = middleName?.Trim();
        LastName = lastName.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? $"{FirstName} {LastName}".Trim()
            : displayName.Trim();
        Phone = phone.Trim();
        AlternatePhone = alternatePhone?.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        DriverType = driverType;
        DateOfBirth = dateOfBirth;
        NationalityCode = nationalityCode?.Trim().ToUpperInvariant();
        PreferredLanguage = preferredLanguage?.Trim();
        Notes = notes?.Trim();
        MarkUpdated();
    }

    public void UpdateDates(DateOnly? hireDate, DateOnly? startDate, DateOnly? endDate)
    {
        if (startDate.HasValue && endDate.HasValue && endDate < startDate)
            throw new ArgumentException("End date cannot be earlier than start date.");

        HireDate = hireDate;
        StartDate = startDate;
        EndDate = endDate;
        MarkUpdated();
    }

    public void AssignBranch(Guid? branchId)
    {
        BranchId = branchId;
        MarkUpdated();
    }

    public void AssignDepartment(Guid? departmentId)
    {
        DepartmentId = departmentId;
        MarkUpdated();
    }

    public void LinkEmployee(Guid? employeeId)
    {
        EmployeeId = employeeId;
        MarkUpdated();
    }

    public void SetStatus(DriverStatus newStatus)
    {
        if (Status == newStatus) return;

        // Terminal state check
        if (Status is DriverStatus.Terminated or DriverStatus.Retired && newStatus != DriverStatus.Active)
        {
            throw new InvalidOperationException($"Driver in terminal status '{Status}' cannot be transitioned to '{newStatus}'.");
        }

        Status = newStatus;
        IsActive = newStatus is DriverStatus.Active or DriverStatus.Draft or DriverStatus.OnLeave;
        MarkUpdated();
    }

    public void SetPrimaryLicenseNumber(string? licenseNumber)
    {
        PrimaryLicenseNumber = licenseNumber?.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void SetProfileImage(string? objectKey)
    {
        ProfileImageObjectKey = objectKey?.Trim();
        MarkUpdated();
    }

    public void Activate()
    {
        SetStatus(DriverStatus.Active);
    }

    public void Deactivate()
    {
        SetStatus(DriverStatus.Inactive);
    }

    public DriverAvailabilityStatus GetAvailabilityStatus()
    {
        if (Status != DriverStatus.Active)
        {
            return DriverAvailabilityStatus.Unavailable;
        }

        var hasActiveAssignment = _vehicleAssignments.Any(a => a.IsCurrentlyActive() && a.IsPrimary);
        return hasActiveAssignment ? DriverAvailabilityStatus.Assigned : DriverAvailabilityStatus.Available;
    }
}
