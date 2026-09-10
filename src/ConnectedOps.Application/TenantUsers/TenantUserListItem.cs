namespace ConnectedOps.Application.TenantUsers;

public sealed record TenantUserListItem(
    Guid TenantUserId,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool IsDefaultTenant,
    DateTime JoinedAtUtc,
    IReadOnlyCollection<string> Roles);