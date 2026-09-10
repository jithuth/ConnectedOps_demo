using ConnectedOps.Domain.Auditing;

namespace ConnectedOps.Application.Auditing;

public sealed record CreateAuditLogRequest(
    AuditAction Action,
    string EntityType,
    string? EntityId = null,
    string? Description = null,
    object? OldValues = null,
    object? NewValues = null);