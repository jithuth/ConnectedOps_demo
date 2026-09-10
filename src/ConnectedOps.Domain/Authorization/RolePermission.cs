using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Authorization;

public sealed class RolePermission : BaseEntity
{
    private RolePermission()
    {
    }

    public RolePermission(
        Guid tenantRoleId,
        Guid permissionId)
    {
        if (tenantRoleId == Guid.Empty)
            throw new ArgumentException(
                "TenantRoleId is required.",
                nameof(tenantRoleId));

        if (permissionId == Guid.Empty)
            throw new ArgumentException(
                "PermissionId is required.",
                nameof(permissionId));

        TenantRoleId = tenantRoleId;
        PermissionId = permissionId;
    }

    public Guid TenantRoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public TenantRole TenantRole { get; private set; } = null!;

    public Permission Permission { get; private set; } = null!;
}