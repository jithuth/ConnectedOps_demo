using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Organization;

public sealed class Department : BaseEntity
{
    private readonly List<Department> _subDepartments = [];
    private readonly List<Team> _teams = [];
    private readonly List<Employee> _employees = [];

    private Department()
    {
    }

    public Department(
        Guid tenantId,
        string name,
        string code,
        Guid? branchId = null,
        Guid? parentDepartmentId = null,
        string? description = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        TenantId = tenantId;
        BranchId = branchId;
        ParentDepartmentId = parentDepartmentId;
        SetName(name);
        SetCode(code);
        Description = description?.Trim();
        IsActive = true;
    }

    public Guid TenantId { get; private set; }
    public Guid? BranchId { get; private set; }
    public Branch? Branch { get; private set; }
    public Guid? ParentDepartmentId { get; private set; }
    public Department? ParentDepartment { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    public IReadOnlyCollection<Department> SubDepartments => _subDepartments.AsReadOnly();
    public IReadOnlyCollection<Team> Teams => _teams.AsReadOnly();
    public IReadOnlyCollection<Employee> Employees => _employees.AsReadOnly();

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name is required.", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Department name cannot exceed 200 characters.", nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Department code is required.", nameof(code));

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length > 50)
            throw new ArgumentException("Department code cannot exceed 50 characters.", nameof(code));

        Code = normalized;
        MarkUpdated();
    }

    public void Update(
        string name,
        string code,
        Guid? branchId,
        Guid? parentDepartmentId,
        string? description)
    {
        if (parentDepartmentId.HasValue && parentDepartmentId.Value == Id)
            throw new InvalidOperationException("A department cannot be its own parent.");

        SetName(name);
        SetCode(code);
        BranchId = branchId;
        ParentDepartmentId = parentDepartmentId;
        Description = description?.Trim();

        MarkUpdated();
    }

    public void SetParent(Guid? parentDepartmentId)
    {
        if (parentDepartmentId.HasValue && parentDepartmentId.Value == Id)
            throw new InvalidOperationException("A department cannot be its own parent.");

        ParentDepartmentId = parentDepartmentId;
        MarkUpdated();
    }

    public void SetBranch(Guid? branchId)
    {
        BranchId = branchId;
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
