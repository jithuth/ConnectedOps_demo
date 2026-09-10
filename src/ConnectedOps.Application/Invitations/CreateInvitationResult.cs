namespace ConnectedOps.Application.Invitations;

public sealed record CreateInvitationResult(
    Guid InvitationId,
    string Email,
    Guid TenantId,
    Guid TenantRoleId,
    string RoleName,
    DateTime ExpiresAtUtc,
    string InvitationToken);