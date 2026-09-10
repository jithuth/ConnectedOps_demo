namespace ConnectedOps.Application.Invitations;

public sealed class CreateInvitationRequest
{
    public string Email { get; init; }
        = string.Empty;

    public Guid TenantRoleId { get; init; }
}