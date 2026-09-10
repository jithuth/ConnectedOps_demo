using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Auditing;

public sealed class AuditLog : BaseEntity
{
    private AuditLog()
    {
    }

    public AuditLog(
        Guid? tenantId,
        Guid? userId,
        Guid? tenantUserId,
        AuditAction action,
        string entityType,
        string? entityId,
        string? description,
        string? oldValues,
        string? newValues,
        string? ipAddress,
        string? userAgent,
        string? requestPath,
        string? traceId)
    {
        if (tenantId.HasValue && tenantId.Value == Guid.Empty)
        {
            throw new ArgumentException(
                "TenantId cannot be empty.",
                nameof(tenantId));
        }

        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException(
                "Entity type is required.",
                nameof(entityType));
        }

        TenantId = tenantId;
        UserId = userId;
        TenantUserId = tenantUserId;

        Action = action;

        EntityType = entityType.Trim();
        EntityId = entityId;

        Description = description;

        OldValues = oldValues;
        NewValues = newValues;

        IpAddress = ipAddress;
        UserAgent = userAgent;

        RequestPath = requestPath;
        TraceId = traceId;
    }

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? TenantUserId { get; private set; }

    public AuditAction Action { get; private set; }

    public string EntityType { get; private set; } =
        string.Empty;

    public string? EntityId { get; private set; }

    public string? Description { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? RequestPath { get; private set; }

    public string? TraceId { get; private set; }
}