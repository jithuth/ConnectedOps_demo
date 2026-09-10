namespace ConnectedOps.Application.Auth;

public sealed class SelectTenantRequest
{
    public string TenantSelectionToken { get; init; }
        = string.Empty;

    public Guid TenantId { get; init; }
}