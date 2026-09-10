namespace ConnectedOps.Application.Auditing;

public sealed class AuditLogQuery
{
    public string? EntityType { get; init; }

    public string? EntityId { get; init; }

    public Guid? UserId { get; init; }

    public string? Action { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}