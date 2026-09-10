using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Security;

public sealed class SecurityLog : BaseEntity
{
    private SecurityLog()
    {
    }

    public SecurityLog(
        Guid? tenantId,
        Guid? userId,
        Guid? tenantUserId,
        SecurityEventType eventType,
        bool succeeded,
        string? email,
        string? description,
        string? ipAddress,
        string? userAgent,
        string? requestPath,
        string? traceId,
        string? metadata)
    {
        TenantId = tenantId;
        UserId = userId;
        TenantUserId = tenantUserId;

        EventType = eventType;
        Succeeded = succeeded;

        Email = Normalize(email);
        Description = Normalize(description);

        IpAddress = Normalize(ipAddress);
        UserAgent = Normalize(userAgent);
        RequestPath = Normalize(requestPath);
        TraceId = Normalize(traceId);

        Metadata = metadata;
    }

    public Guid? TenantId { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? TenantUserId { get; private set; }

    public SecurityEventType EventType { get; private set; }

    public bool Succeeded { get; private set; }

    public string? Email { get; private set; }

    public string? Description { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? RequestPath { get; private set; }

    public string? TraceId { get; private set; }

    public string? Metadata { get; private set; }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}