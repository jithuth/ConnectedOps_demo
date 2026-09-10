namespace ConnectedOps.Application.Auth;

public sealed record CurrentUserResult(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    Guid TenantId,
    Guid TenantUserId,
    string TenantName,
    string TenantCode,
    bool IsDefaultTenant,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);