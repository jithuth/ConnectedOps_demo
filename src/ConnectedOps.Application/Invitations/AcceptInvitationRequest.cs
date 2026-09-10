namespace ConnectedOps.Application.Invitations;

public sealed class AcceptInvitationRequest
{
    public string Token { get; init; }
        = string.Empty;

    public string FirstName { get; init; }
        = string.Empty;

    public string LastName { get; init; }
        = string.Empty;

    public string Password { get; init; }
        = string.Empty;
}