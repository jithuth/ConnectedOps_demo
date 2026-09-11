using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class Employee : BaseEntity
{
    private readonly List<Employee> _directReports = [];

    private Employee()
    {
    }

    public Employee(
        Guid tenantId,
        string employeeNumber,
        string firstName,
        string lastName,
        string email,
        string? phone = null,
        string? jobTitle = null,
        EmploymentType employmentType = EmploymentType.FullTime,
        EmploymentStatus employmentStatus = EmploymentStatus.Active,
        Guid? branchId = null,
        Guid? departmentId = null,
        Guid? teamId = null,
        Guid? managerEmployeeId = null,
        Guid? userId = null,
        DateOnly? hireDate = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetEmployeeNumber(employeeNumber);
        SetName(firstName, lastName);
        SetEmail(email);

        Phone = phone?.Trim();
        JobTitle = jobTitle?.Trim();
        EmploymentType = employmentType;
        EmploymentStatus = employmentStatus;
        BranchId = branchId;
        DepartmentId = departmentId;
        TeamId = teamId;
        ManagerEmployeeId = managerEmployeeId;
        UserId = userId;
        HireDate = hireDate;
    }

    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Department? Department { get; private set; }
    public Guid? TeamId { get; private set; }
    public Team? Team { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }
    public Employee? Manager { get; private set; }

    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? JobTitle { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public EmploymentStatus EmploymentStatus { get; private set; }
    public DateOnly? HireDate { get; private set; }
    public DateOnly? TerminationDate { get; private set; }

    public bool IsActive => EmploymentStatus == EmploymentStatus.Active;

    public IReadOnlyCollection<Employee> DirectReports => _directReports.AsReadOnly();

    public void SetEmployeeNumber(string employeeNumber)
    {
        if (string.IsNullOrWhiteSpace(employeeNumber))
            throw new ArgumentException("Employee number is required.", nameof(employeeNumber));

        var normalized = employeeNumber.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new ArgumentException("Employee number cannot exceed 50 characters.", nameof(employeeNumber));

        EmployeeNumber = normalized;
        MarkUpdated();
    }

    public void SetName(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required.", nameof(lastName));

        if (firstName.Length > 100)
            throw new ArgumentException("First name cannot exceed 100 characters.", nameof(firstName));
        if (lastName.Length > 100)
            throw new ArgumentException("Last name cannot exceed 100 characters.", nameof(lastName));

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkUpdated();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length > 250)
            throw new ArgumentException("Email cannot exceed 250 characters.", nameof(email));

        Email = normalized;
        MarkUpdated();
    }

    public void Update(
        string employeeNumber,
        string firstName,
        string lastName,
        string email,
        string? phone,
        string? jobTitle,
        EmploymentType employmentType,
        EmploymentStatus employmentStatus,
        Guid? branchId,
        Guid? departmentId,
        Guid? teamId,
        Guid? managerEmployeeId,
        DateOnly? hireDate,
        DateOnly? terminationDate)
    {
        if (managerEmployeeId.HasValue && managerEmployeeId.Value == Id)
            throw new InvalidOperationException("An employee cannot be their own manager.");

        SetEmployeeNumber(employeeNumber);
        SetName(firstName, lastName);
        SetEmail(email);

        Phone = phone?.Trim();
        JobTitle = jobTitle?.Trim();
        EmploymentType = employmentType;
        EmploymentStatus = employmentStatus;
        BranchId = branchId;
        DepartmentId = departmentId;
        TeamId = teamId;
        ManagerEmployeeId = managerEmployeeId;
        HireDate = hireDate;
        TerminationDate = terminationDate;

        MarkUpdated();
    }

    public void LinkUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        UserId = userId;
        MarkUpdated();
    }

    public void UnlinkUser()
    {
        UserId = null;
        MarkUpdated();
    }

    public void SetManager(Guid? managerEmployeeId)
    {
        if (managerEmployeeId.HasValue && managerEmployeeId.Value == Id)
            throw new InvalidOperationException("An employee cannot be their own manager.");

        ManagerEmployeeId = managerEmployeeId;
        MarkUpdated();
    }

    public void SetOrganizationPlacement(Guid? branchId, Guid? departmentId, Guid? teamId)
    {
        BranchId = branchId;
        DepartmentId = departmentId;
        TeamId = teamId;
        MarkUpdated();
    }

    public void SetStatus(EmploymentStatus status)
    {
        EmploymentStatus = status;
        MarkUpdated();
    }

    public void Terminate(DateOnly terminationDate)
    {
        EmploymentStatus = EmploymentStatus.Terminated;
        TerminationDate = terminationDate;
        MarkUpdated();
    }
}
