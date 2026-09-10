namespace ConnectedOps.Application.Invitations;

public interface IInvitationService
{
    Task<CreateInvitationResult> CreateAsync(
        CreateInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task<AcceptInvitationResult> AcceptAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default);
}