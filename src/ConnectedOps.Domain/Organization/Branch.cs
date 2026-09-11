using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class Branch : BaseEntity
{
    private readonly List<Location> _locations = [];
    private readonly List<Department> _departments = [];
    private readonly List<Employee> _employees = [];

    private Branch()
    {
    }

    public Branch(
        Guid tenantId,
        string name,
        string code,
        BranchType type = BranchType.Depot,
        bool isHeadOffice = false,
        string? email = null,
        string? phone = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? stateOrProvince = null,
        string? postalCode = null,
        string? countryCode = null,
        string? timeZoneId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        SetCode(code);
        Type = type;
        IsHeadOffice = isHeadOffice;
        IsActive = true;

        UpdateContactAndAddress(
            email,
            phone,
            addressLine1,
            addressLine2,
            city,
            stateOrProvince,
            postalCode,
            countryCode,
            timeZoneId);
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public BranchType Type { get; private set; }
    public bool IsHeadOffice { get; private set; }
    public bool IsActive { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? StateOrProvince { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string? TimeZoneId { get; private set; }

    public IReadOnlyCollection<Location> Locations => _locations.AsReadOnly();
    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();
    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Branch name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Branch name cannot exceed 200 characters.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Branch code is required.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new ArgumentException("Branch code cannot exceed 50 characters.", nameof(code));

        Code = normalized;
        MarkUpdated();
    }

    public void Update(
        string name,
        string code,
        BranchType type,
        bool isHeadOffice,
        string? email,
        string? phone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? countryCode,
        string? timeZoneId)
    {
        SetName(name);
        SetCode(code);
        Type = type;
        IsHeadOffice = isHeadOffice;

        UpdateContactAndAddress(
            email,
            phone,
            addressLine1,
            addressLine2,
            city,
            stateOrProvince,
            postalCode,
            countryCode,
            timeZoneId);
    }

    public void UpdateContactAndAddress(
        string? email,
        string? phone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? stateOrProvince,
        string? postalCode,
        string? countryCode,
        string? timeZoneId)
    {
        Email = email?.Trim();
        Phone = phone?.Trim();
        AddressLine1 = addressLine1?.Trim();
        AddressLine2 = addressLine2?.Trim();
        City = city?.Trim();
        StateOrProvince = stateOrProvince?.Trim();
        PostalCode = postalCode?.Trim();
        CountryCode = countryCode?.Trim()?.ToUpperInvariant();
        TimeZoneId = timeZoneId?.Trim();

        MarkUpdated();
    }

    public void SetHeadOffice(bool isHeadOffice)
    {
        IsHeadOffice = isHeadOffice;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}
