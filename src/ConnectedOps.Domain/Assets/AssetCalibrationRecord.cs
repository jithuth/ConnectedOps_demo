using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Assets;

public sealed class AssetCalibrationRecord : BaseEntity
{
    private AssetCalibrationRecord()
    {
    }

    public AssetCalibrationRecord(
        Guid tenantId,
        Guid assetId,
        DateTime calibrationDateUtc,
        DateTime? nextCalibrationDateUtc = null,
        string? certificateNumber = null,
        string? provider = null,
        string result = "Passed",
        string? documentObjectKey = null,
        string? notes = null,
        Guid? createdByUserId = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (assetId == Guid.Empty)
            throw new ArgumentException("AssetId is required.", nameof(assetId));

        TenantId = tenantId;
        AssetId = assetId;
        CalibrationDateUtc = calibrationDateUtc;
        NextCalibrationDateUtc = nextCalibrationDateUtc;
        CertificateNumber = certificateNumber?.Trim();
        Provider = provider?.Trim();
        Result = string.IsNullOrWhiteSpace(result) ? "Passed" : result.Trim();
        DocumentObjectKey = documentObjectKey?.Trim();
        Notes = notes?.Trim();
        CreatedBy = createdByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid AssetId { get; private set; }
    public Asset Asset { get; private set; } = null!;
    public DateTime CalibrationDateUtc { get; private set; }
    public DateTime? NextCalibrationDateUtc { get; private set; }
    public string? CertificateNumber { get; private set; }
    public string? Provider { get; private set; }
    public string Result { get; private set; } = "Passed";
    public string? DocumentObjectKey { get; private set; }
    public string? Notes { get; private set; }
}
