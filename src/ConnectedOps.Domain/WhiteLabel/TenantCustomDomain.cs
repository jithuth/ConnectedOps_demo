using System.Security.Cryptography;
using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.WhiteLabel;

public sealed class TenantCustomDomain : BaseEntity
{
    private TenantCustomDomain()
    {
    }

    public TenantCustomDomain(
        Guid tenantId,
        string hostname)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(hostname))
            throw new ArgumentException("Hostname is required.", nameof(hostname));

        TenantId = tenantId;
        Hostname = hostname.Trim().ToLowerInvariant();
        VerificationToken = $"co-verify-{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant()}";
        Status = DomainVerificationStatus.PendingDns;
        SslProvisioned = false;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public string Hostname { get; private set; } = string.Empty;
    public string VerificationToken { get; private set; } = string.Empty;
    public DomainVerificationStatus Status { get; private set; }
    public bool SslProvisioned { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }

    public void MarkVerified()
    {
        Status = DomainVerificationStatus.Verified;
        SslProvisioned = true;
        VerifiedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkFailed()
    {
        Status = DomainVerificationStatus.Failed;
        MarkUpdated();
    }
}
