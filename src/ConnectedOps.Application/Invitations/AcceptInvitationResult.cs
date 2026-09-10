namespace ConnectedOps.Application.Invitations;

public sealed record AcceptInvitationResult(
    Guid UserId,
    Guid TenantId,
    Guid TenantUserId,
    string TenantName,
    string RoleCode);