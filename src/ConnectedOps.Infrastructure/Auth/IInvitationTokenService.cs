namespace ConnectedOps.Infrastructure.Auth;

public interface IInvitationTokenService
{
    string GenerateToken();

    string HashToken(
        string token);
}