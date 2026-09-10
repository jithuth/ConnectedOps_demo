using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Tenancy;

public sealed class TenantUser : BaseEntity
{
    private TenantUser()
    {
    }

    public TenantUser(
        Guid tenantId,
        Guid userId,
        bool isDefaultTenant = false)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId is required.",
                nameof(tenantId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId is required.",
                nameof(userId));
        }

        TenantId = tenantId;
        UserId = userId;
        IsDefaultTenant = isDefaultTenant;
        IsActive = true;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }

    public Guid UserId { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsDefaultTenant { get; private set; }

    public DateTime JoinedAtUtc { get; private set; }

    public Tenant Tenant { get; private set; } = null!;

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

    public void SetAsDefaultTenant()
    {
        IsDefaultTenant = true;
        MarkUpdated();
    }

    public void RemoveDefaultTenant()
    {
        IsDefaultTenant = false;
        MarkUpdated();
    }
}