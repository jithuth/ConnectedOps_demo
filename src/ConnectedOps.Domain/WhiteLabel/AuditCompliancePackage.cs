using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.WhiteLabel;

public sealed class AuditCompliancePackage : BaseEntity
{
    private AuditCompliancePackage()
    {
    }

    public AuditCompliancePackage(
        Guid tenantId,
        string packageNumber,
        AuditPackageType packageType,
        Guid generatedByUserId,
        int evidenceItemsCount,
        string checksumSha256,
        long fileSizeBytes,
        string manifestJson)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(packageNumber))
            throw new ArgumentException("PackageNumber is required.", nameof(packageNumber));

        TenantId = tenantId;
        PackageNumber = packageNumber.Trim().ToUpperInvariant();
        PackageType = packageType;
        GeneratedByUserId = generatedByUserId;
        EvidenceItemsCount = evidenceItemsCount;
        ChecksumSha256 = checksumSha256.Trim();
        FileSizeBytes = Math.Max(0, fileSizeBytes);
        ManifestJson = manifestJson ?? "{}";
        GeneratedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public string PackageNumber { get; private set; } = string.Empty;
    public AuditPackageType PackageType { get; private set; }
    public DateTime GeneratedAtUtc { get; private set; }
    public Guid GeneratedByUserId { get; private set; }
    public int EvidenceItemsCount { get; private set; }
    public string ChecksumSha256 { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public string ManifestJson { get; private set; } = string.Empty;
}
