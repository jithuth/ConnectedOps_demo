namespace ConnectedOps.Application.Tenants;

public sealed class CreateTenantRequest
{
    public string CompanyName { get; init; } = string.Empty;

    public string CompanyCode { get; init; } = string.Empty;

    public string? CompanyEmail { get; init; }

    public string OwnerFirstName { get; init; } = string.Empty;

    public string OwnerLastName { get; init; } = string.Empty;

    public string OwnerEmail { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}