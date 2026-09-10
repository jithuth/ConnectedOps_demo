using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Auth;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken()
    {
    }

    public RefreshToken(
        Guid userId,
        Guid? tenantId,
        Guid? tenantUserId,
        string tokenHash,
        Guid familyId,
        DateTime expiresAtUtc,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if ((tenantId.HasValue && !tenantUserId.HasValue) || (!tenantId.HasValue && tenantUserId.HasValue))
            throw new ArgumentException("TenantId and TenantUserId must both be provided for tenant tokens or both be null for platform tokens.");

        if (tenantId.HasValue && tenantId.Value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(tenantId));

        if (tenantUserId.HasValue && tenantUserId.Value == Guid.Empty)
            throw new ArgumentException("TenantUserId cannot be empty.", nameof(tenantUserId));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));

        if (familyId == Guid.Empty)
            throw new ArgumentException("FamilyId is required.", nameof(familyId));

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("Expiry must be in the future.", nameof(expiresAtUtc));

        UserId = userId;
        TenantId = tenantId;
        TenantUserId = tenantUserId;
        TokenHash = tokenHash;
        FamilyId = familyId;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp?.Trim();
    }

    public Guid UserId { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? TenantUserId { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public Guid FamilyId { get; private set; }

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? UsedAtUtc { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public bool IsExpired =>
        DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsUsed =>
        UsedAtUtc.HasValue;

    public bool IsActive =>
        !IsExpired &&
        !IsRevoked &&
        !IsUsed &&
        !IsDeleted;

    public void MarkUsed()
    {
        if (UsedAtUtc.HasValue)
            return;

        UsedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void ReplaceWith(Guid replacementTokenId)
    {
        if (replacementTokenId == Guid.Empty)
            throw new ArgumentException(
                "Replacement token ID is required.",
                nameof(replacementTokenId));

        ReplacedByTokenId = replacementTokenId;
        MarkUpdated();
    }

    public void Revoke(string? revokedByIp = null)
    {
        if (RevokedAtUtc.HasValue)
            return;

        RevokedAtUtc = DateTime.UtcNow;
        RevokedByIp = revokedByIp?.Trim();
        MarkUpdated();
    }
}