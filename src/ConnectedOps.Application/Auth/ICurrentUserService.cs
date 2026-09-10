namespace ConnectedOps.Application.Auth;

public interface ICurrentUserService
{
    Task<CurrentUserResult> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}