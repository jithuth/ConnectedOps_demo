namespace ConnectedOps.Application.Auditing;

public sealed record AuditLogPage(
    IReadOnlyList<AuditLogDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}