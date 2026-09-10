using ConnectedOps.Domain.Platform;

using Microsoft.AspNetCore.Identity;

namespace ConnectedOps.Infrastructure.Identity;

public sealed class ApplicationUser
    : IdentityUser<Guid>
{
    public ApplicationUser()
    {
        Id =
            Guid.NewGuid();

        SecurityStamp =
            Guid.NewGuid()
                .ToString();
    }

    public string FirstName
    { get; set; } =
        string.Empty;

    public string LastName
    { get; set; } =
        string.Empty;

    public bool IsActive
    { get; set; } =
        true;

    public PlatformRole PlatformRole
    { get; set; } =
        PlatformRole.None;

    public DateTime CreatedAtUtc
    { get; set; } =
        DateTime.UtcNow;

    public DateTime? LastLoginAtUtc
    { get; set; }

    public string FullName =>
        $"{FirstName} {LastName}".Trim();
}