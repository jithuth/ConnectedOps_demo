namespace ConnectedOps.Application.Permissions;

public sealed record PermissionListItem(
    Guid Id,
    string Key,
    string Name,
    string? Description);