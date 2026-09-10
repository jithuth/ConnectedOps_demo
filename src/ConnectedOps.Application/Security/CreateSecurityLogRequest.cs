using ConnectedOps.Domain.Security;

namespace ConnectedOps.Application.Security;

public sealed record CreateSecurityLogRequest(
    SecurityEventType EventType,
    bool Succeeded,
    string? Email = null,
    string? Description = null,
    Guid? TenantId = null,
    Guid? UserId = null,
    Guid? TenantUserId = null,
    object? Metadata = null);