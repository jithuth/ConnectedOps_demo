namespace ConnectedOps.Application.TenantUsers;

public sealed record TenantUserDetailsResult(
    Guid TenantUserId,
    Guid UserId,
    Guid TenantId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    bool IsActive,
    bool IsDefaultTenant,
    DateTime JoinedAtUtc,
    IReadOnlyCollection<TenantUserRoleResult> Roles,
    IReadOnlyCollection<string> Permissions);