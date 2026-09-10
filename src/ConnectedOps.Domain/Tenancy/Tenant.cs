using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Tenancy;

public sealed class Tenant : BaseEntity
{
    private Tenant()
    {
    }

    public Tenant(
        string name,
        string code,
        string? email = null)
    {
        SetName(name);
        SetCode(code);

        Email = email?.Trim();
        Status = TenantStatus.Active;
    }

    private readonly List<TenantUser> _users = [];

    public IReadOnlyCollection<TenantUser> Users =>
        _users.AsReadOnly();

    public string Name { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? CountryCode { get; private set; }
    public TenantSettings? Settings { get; private set; }

    public TenantStatus Status { get; private set; }

    public bool IsActive => Status == TenantStatus.Active;

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Tenant name is required.",
                nameof(name));

        if (name.Length > 200)
            throw new ArgumentException(
                "Tenant name cannot exceed 200 characters.",
                nameof(name));

        Name = name.Trim();

        MarkUpdated();
    }

    public void SetCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException(
                "Tenant code is required.",
                nameof(code));

        var normalizedCode = code
            .Trim()
            .ToUpperInvariant();

        if (normalizedCode.Length > 50)
            throw new ArgumentException(
                "Tenant code cannot exceed 50 characters.",
                nameof(code));

        Code = normalizedCode;

        MarkUpdated();
    }

    public void UpdateContact(
        string? email,
        string? phone,
        string? countryCode)
    {
        Email = email?.Trim();
        Phone = phone?.Trim();
        CountryCode = countryCode?.Trim()?.ToUpperInvariant();

        MarkUpdated();
    }

    public void Activate()
    {
        Status = TenantStatus.Active;
        MarkUpdated();
    }

    public void Suspend()
    {
        Status = TenantStatus.Suspended;
        MarkUpdated();
    }

    public void Disable()
    {
        Status = TenantStatus.Disabled;
        MarkUpdated();
    }
}