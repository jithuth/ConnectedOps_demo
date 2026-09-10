using ConnectedOps.Domain.Common;
using ConnectedOps.Domain.Tenancy;

namespace ConnectedOps.Domain.Authorization;

public sealed class TenantUserRole : BaseEntity
{
    private TenantUserRole()
    {
    }

    public TenantUserRole(
        Guid tenantUserId,
        Guid tenantRoleId)
    {
        if (tenantUserId == Guid.Empty)
            throw new ArgumentException(
                "TenantUserId is required.",
                nameof(tenantUserId));

        if (tenantRoleId == Guid.Empty)
            throw new ArgumentException(
                "TenantRoleId is required.",
                nameof(tenantRoleId));

        TenantUserId = tenantUserId;
        TenantRoleId = tenantRoleId;
    }

    public Guid TenantUserId { get; private set; }

    public Guid TenantRoleId { get; private set; }

    public TenantUser TenantUser { get; private set; } = null!;

    public TenantRole TenantRole { get; private set; } = null!;
}