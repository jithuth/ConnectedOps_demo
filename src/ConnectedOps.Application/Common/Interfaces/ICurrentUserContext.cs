namespace ConnectedOps.Application.Common.Interfaces;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    Guid? TenantId { get; }

    Guid? TenantUserId { get; }
}