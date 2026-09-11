using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class TrackingProvider : BaseEntity
{
    private TrackingProvider()
    {
    }

    public TrackingProvider(
        string name,
        string code,
        ProviderType providerType,
        string? description = null,
        Guid? tenantId = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Provider code is required.", nameof(code));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        ProviderType = providerType;
        Description = description?.Trim();
        TenantId = tenantId;
        IsActive = isActive;
    }

    public Guid? TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public ProviderType ProviderType { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, ProviderType providerType, string? description = null, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Provider name is required.", nameof(name));

        Name = name.Trim();
        ProviderType = providerType;
        Description = description?.Trim();
        MarkUpdated(userId);
    }

    public void Activate(Guid? userId = null)
    {
        IsActive = true;
        MarkUpdated(userId);
    }

    public void Deactivate(Guid? userId = null)
    {
        IsActive = false;
        MarkUpdated(userId);
    }
}
