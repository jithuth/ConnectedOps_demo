using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Safety;

public sealed class SafetyIncidentEvidence : BaseEntity
{
    private SafetyIncidentEvidence()
    {
    }

    public SafetyIncidentEvidence(
        Guid tenantId,
        Guid safetyIncidentId,
        SafetyEvidenceType evidenceType,
        string title,
        string fileObjectKey,
        string fileName,
        string contentType,
        long fileSizeBytes,
        DateTime? capturedAtUtc = null,
        DateTime? uploadedAtUtc = null,
        Guid? uploadedByUserId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (safetyIncidentId == Guid.Empty)
            throw new ArgumentException("SafetyIncidentId is required.", nameof(safetyIncidentId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Evidence title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(fileObjectKey))
            throw new ArgumentException("FileObjectKey is required.", nameof(fileObjectKey));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));

        TenantId = tenantId;
        SafetyIncidentId = safetyIncidentId;
        EvidenceType = evidenceType;
        Title = title.Trim();
        FileObjectKey = fileObjectKey.Trim();
        FileName = fileName.Trim();
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        CapturedAtUtc = capturedAtUtc;
        UploadedAtUtc = uploadedAtUtc ?? DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
        Notes = notes?.Trim();
        CreatedBy = uploadedByUserId;
    }

    public Guid TenantId { get; private set; }
    public Guid SafetyIncidentId { get; private set; }
    public SafetyIncident SafetyIncident { get; private set; } = null!;

    public SafetyEvidenceType EvidenceType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string FileObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long FileSizeBytes { get; private set; }
    public DateTime? CapturedAtUtc { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public string? Notes { get; private set; }
}
