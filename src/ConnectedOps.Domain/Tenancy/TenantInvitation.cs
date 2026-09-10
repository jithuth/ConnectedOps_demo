using ConnectedOps.Domain.Authorization;
using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Tenancy;

public sealed class TenantInvitation : BaseEntity
{
    private TenantInvitation()
    {
    }

    public TenantInvitation(
        Guid tenantId,
        string email,
        Guid tenantRoleId,
        Guid invitedByUserId,
        string tokenHash,
        DateTime expiresAtUtc)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException(
                "TenantId is required.",
                nameof(tenantId));

        if (tenantRoleId == Guid.Empty)
            throw new ArgumentException(
                "TenantRoleId is required.",
                nameof(tenantRoleId));

        if (invitedByUserId == Guid.Empty)
            throw new ArgumentException(
                "InvitedByUserId is required.",
                nameof(invitedByUserId));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException(
                "Email is required.",
                nameof(email));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException(
                "Token hash is required.",
                nameof(tokenHash));

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException(
                "Invitation expiry must be in the future.",
                nameof(expiresAtUtc));

        TenantId = tenantId;
        Email = email.Trim().ToLowerInvariant();
        TenantRoleId = tenantRoleId;
        InvitedByUserId = invitedByUserId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid TenantId { get; private set; }

    public string Email { get; private set; }
        = string.Empty;

    public Guid TenantRoleId { get; private set; }

    public Guid InvitedByUserId { get; private set; }

    public string TokenHash { get; private set; }
        = string.Empty;

    public DateTime ExpiresAtUtc { get; private set; }

    public DateTime? AcceptedAtUtc { get; private set; }

    public Guid? AcceptedByUserId { get; private set; }

    public DateTime? RevokedAtUtc { get; private set; }

    public Guid? RevokedByUserId { get; private set; }

    public bool IsExpired =>
        DateTime.UtcNow >= ExpiresAtUtc;

    public bool IsAccepted =>
        AcceptedAtUtc.HasValue;

    public bool IsRevoked =>
        RevokedAtUtc.HasValue;

    public bool IsActive =>
        !IsExpired &&
        !IsAccepted &&
        !IsRevoked &&
        !IsDeleted;

    public Tenant Tenant { get; private set; }
        = null!;

    public TenantRole TenantRole { get; private set; }
        = null!;

    public void Accept(
        Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "UserId is required.",
                nameof(userId));

        if (AcceptedAtUtc.HasValue)
            throw new InvalidOperationException(
                "Invitation has already been accepted.");

        if (RevokedAtUtc.HasValue)
            throw new InvalidOperationException(
                "Invitation has been revoked.");

        if (DateTime.UtcNow >= ExpiresAtUtc)
            throw new InvalidOperationException(
                "Invitation has expired.");

        AcceptedAtUtc = DateTime.UtcNow;
        AcceptedByUserId = userId;

        MarkUpdated(userId);
    }

    public void Revoke(
        Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "UserId is required.",
                nameof(userId));

        if (AcceptedAtUtc.HasValue)
            throw new InvalidOperationException(
                "Accepted invitations cannot be revoked.");

        if (RevokedAtUtc.HasValue)
            return;

        RevokedAtUtc = DateTime.UtcNow;
        RevokedByUserId = userId;

        MarkUpdated(userId);
    }
}