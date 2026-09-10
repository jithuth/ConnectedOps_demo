using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Domain.Authorization;

public sealed class TenantRole : BaseEntity
{
    private readonly List<RolePermission> _permissions = [];
    private readonly List<TenantUserRole> _users = [];

    private TenantRole()
    {
    }

    public TenantRole(
        Guid tenantId,
        string name,
        string code,
        string? description = null,
        bool isSystemRole = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "TenantId is required.",
                nameof(tenantId));

        TenantId = tenantId;

        SetName(name);
        SetCode(code);

        Description = description?.Trim();
        IsSystemRole = isSystemRole;
        IsActive = true;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsSystemRole { get; private set; }

    public bool IsActive { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

    public IReadOnlyCollection<RolePermission> Permissions =>
        _permissions.AsReadOnly();

    public IReadOnlyCollection<TenantUserRole> Users =>
        _users.AsReadOnly();

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Role name is required.",
                nameof(name));

        Name = name.Trim();
        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Role code is required.",
                nameof(code));

        Code = code
            .Trim()
            .ToUpperInvariant();

        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }

    public void Rename(string name)
    {
        if (IsSystemRole)
        {
            throw new InvalidOperationException(
                "System roles cannot be renamed.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Role name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > 100)
        {
            throw new ArgumentException(
                "Role name cannot exceed 100 characters.",
                nameof(name));
        }

        Name = normalizedName;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }
}