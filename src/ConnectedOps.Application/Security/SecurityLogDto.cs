namespace ConnectedOps.Application.Security;

public sealed record SecurityLogDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    Guid? TenantUserId,
    string EventType,
    bool Succeeded,
    string? Email,
    string? Description,
    string? IpAddress,
    string? UserAgent,
    string? RequestPath,
    string? TraceId,
    string? Metadata,
    DateTime CreatedAtUtc);