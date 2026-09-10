namespace ConnectedOps.Application.Security;

public sealed class SecurityLogQuery
{
    public Guid? TenantId { get; init; }

    public Guid? UserId { get; init; }

    public string? Email { get; init; }

    public string? EventType { get; init; }

    public bool? Succeeded { get; init; }

    public string? IpAddress { get; init; }

    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}