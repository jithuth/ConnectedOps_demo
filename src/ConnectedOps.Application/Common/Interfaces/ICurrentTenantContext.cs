namespace ConnectedOps.Application.Common.Interfaces;

public interface ICurrentTenantContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? TenantId { get; }

    Guid? TenantUserId { get; }
}