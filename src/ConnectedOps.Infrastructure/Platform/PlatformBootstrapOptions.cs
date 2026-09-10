namespace ConnectedOps.Infrastructure.Platform;

public sealed class PlatformBootstrapOptions
{
    public const string SectionName = "PlatformBootstrap";

    public bool Enabled { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = "Platform";

    public string LastName { get; set; } = "SuperAdmin";
}
