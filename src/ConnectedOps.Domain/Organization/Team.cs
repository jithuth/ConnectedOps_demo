using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class Team : BaseEntity
{
    private readonly List<Employee> _employees = [];

    private Team()
    {
    }

    public Team(
        Guid tenantId,
        Guid departmentId,
        string name,
        string code,
        string? description = null,
        Guid? teamLeadEmployeeId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (departmentId == Guid.Empty)
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));

        TenantId = tenantId;
        DepartmentId = departmentId;
        SetName(name);
        SetCode(code);
        Description = description?.Trim();
        TeamLeadEmployeeId = teamLeadEmployeeId;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? TeamLeadEmployeeId { get; private set; }
    public Employee? TeamLead { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Team name cannot exceed 200 characters.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Team code is required.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new ArgumentException("Team code cannot exceed 50 characters.", nameof(code));

        Code = normalized;
        MarkUpdated();
    }

    public void Update(
        Guid departmentId,
        string name,
        string code,
        string? description,
        Guid? teamLeadEmployeeId)
    {
        if (departmentId == Guid.Empty)
            throw new ArgumentException("DepartmentId is required.", nameof(departmentId));

        DepartmentId = departmentId;
        SetName(name);
        SetCode(code);
        Description = description?.Trim();
        TeamLeadEmployeeId = teamLeadEmployeeId;

        MarkUpdated();
    }

    public void SetTeamLead(Guid? teamLeadEmployeeId)
    {
        TeamLeadEmployeeId = teamLeadEmployeeId;
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
