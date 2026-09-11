using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Telematics;

public sealed class TelematicsProviderConfiguration : BaseEntity
{
    private TelematicsProviderConfiguration()
    {
    }

    public TelematicsProviderConfiguration(
        Guid tenantId,
        Guid providerId,
        string? endpoint = null,
        string? username = null,
        string? secretReference = null,
        string? apiKeyHash = null,
        bool isActive = true)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (providerId == Guid.Empty)
            throw new ArgumentException("ProviderId is required.", nameof(providerId));

        TenantId = tenantId;
        ProviderId = providerId;
        Endpoint = endpoint?.Trim();
        Username = username?.Trim();
        SecretReference = secretReference?.Trim();
        ApiKeyHash = apiKeyHash?.Trim();
        IsActive = isActive;
    }

    public Guid TenantId { get; private set; }
    public Guid ProviderId { get; private set; }
    public TrackingProvider Provider { get; private set; } = null!;
    public string? Endpoint { get; private set; }
    public string? Username { get; private set; }
    public string? SecretReference { get; private set; }
    public string? ApiKeyHash { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(
        string? endpoint,
        string? username,
        string? secretReference,
        string? apiKeyHash,
        bool isActive,
        Guid? userId = null)
    {
        Endpoint = endpoint?.Trim();
        Username = username?.Trim();
        if (!string.IsNullOrWhiteSpace(secretReference))
        {
            SecretReference = secretReference.Trim();
        }
        if (!string.IsNullOrWhiteSpace(apiKeyHash))
        {
            ApiKeyHash = apiKeyHash.Trim();
        }
        IsActive = isActive;
        MarkUpdated(userId);
    }
}
