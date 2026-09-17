using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Integrations;

public sealed class TenantApiKey : BaseEntity
{
    private TenantApiKey()
    {
    }

    public TenantApiKey(
        Guid tenantId,
        string name,
        string keyPrefix,
        string keyHash,
        IEnumerable<ApiKeyScope> scopes,
        DateTime? expiresAtUtc = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(keyPrefix))
            throw new ArgumentException("KeyPrefix is required.", nameof(keyPrefix));
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new ArgumentException("KeyHash is required.", nameof(keyHash));

        TenantId = tenantId;
        Name = name.Trim();
        KeyPrefix = keyPrefix.Trim();
        KeyHash = keyHash.Trim();
        ExpiresAtUtc = expiresAtUtc;
        IsActive = true;

        SetScopes(scopes);
    }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string KeyPrefix { get; private set; } = string.Empty;
    public string KeyHash { get; private set; } = string.Empty;
    public string Scopes { get; private set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsExpired => ExpiresAtUtc.HasValue && DateTime.UtcNow > ExpiresAtUtc.Value;
    public bool IsRevoked => RevokedAtUtc.HasValue || !IsActive;
    public bool IsValid => !IsExpired && !IsRevoked;

    public IReadOnlyCollection<ApiKeyScope> GetScopes()
    {
        if (string.IsNullOrWhiteSpace(Scopes))
            return [];

        return Scopes
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Enum.TryParse<ApiKeyScope>(s, out var sc) ? sc : (ApiKeyScope?)null)
            .Where(sc => sc.HasValue)
            .Select(sc => sc!.Value)
            .ToList();
    }

    public void SetScopes(IEnumerable<ApiKeyScope> scopes)
    {
        var distinct = scopes.Distinct().ToList();
        Scopes = string.Join(",", distinct);
    }

    public bool HasScope(ApiKeyScope scope)
    {
        return GetScopes().Contains(scope);
    }

    public void RecordUse()
    {
        LastUsedAtUtc = DateTime.UtcNow;
    }

    public void Revoke(Guid? revokedByUserId = null)
    {
        IsActive = false;
        RevokedAtUtc = DateTime.UtcNow;
        MarkUpdated(revokedByUserId);
    }
}
