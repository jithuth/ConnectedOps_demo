namespace ConnectedOps.Application.Auditing;

public sealed record AuditLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    Guid? TenantUserId,
    string Action,
    string EntityType,
    string? EntityId,
    string? Description,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    string? RequestPath,
    string? TraceId,
    DateTime CreatedAtUtc);