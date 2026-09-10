using ConnectedOps.Domain.Platform;

namespace ConnectedOps.Application.Platform;

public sealed record PlatformUserDto(
    Guid Id,
    string Email,
    string FullName,
    string FirstName,
    string LastName,
    PlatformRole PlatformRole,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);

public sealed record PlatformUserDetailDto(
    Guid Id,
    string Email,
    string FullName,
    string FirstName,
    string LastName,
    PlatformRole PlatformRole,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc,
    int TenantMembershipCount);

public sealed record PlatformUserQuery(
    string? Search = null,
    PlatformRole? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 10);

public sealed record PlatformUserPage(
    IReadOnlyCollection<PlatformUserDto> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public sealed record CreatePlatformUserRequest(
    string Email,
    string FirstName,
    string LastName,
    PlatformRole Role,
    string? InitialPassword = null);

public sealed record CreatePlatformUserResult(
    PlatformUserDto User,
    string? GeneratedPassword);

public sealed record UpdatePlatformUserRoleRequest(
    PlatformRole Role);

public interface IPlatformUserService
{
    Task<PlatformUserPage> GetUsersAsync(
        PlatformUserQuery query,
        CancellationToken cancellationToken = default);

    Task<PlatformUserDetailDto?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CreatePlatformUserResult> CreateUserAsync(
        CreatePlatformUserRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateUserRoleAsync(
        Guid userId,
        PlatformRole role,
        CancellationToken cancellationToken = default);

    Task ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
