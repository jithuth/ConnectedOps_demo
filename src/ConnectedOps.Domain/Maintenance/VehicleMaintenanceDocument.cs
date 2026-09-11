using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Maintenance;

public sealed class VehicleMaintenanceDocument : BaseEntity
{
    private VehicleMaintenanceDocument()
    {
    }

    public VehicleMaintenanceDocument(
        Guid tenantId,
        Guid maintenanceRecordId,
        MaintenanceDocumentType documentType,
        string title,
        string fileObjectKey,
        string fileName,
        string? contentType = null,
        long? fileSizeBytes = null,
        DateTime? uploadedAtUtc = null,
        Guid? uploadedByUserId = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (maintenanceRecordId == Guid.Empty)
            throw new ArgumentException("MaintenanceRecordId is required.", nameof(maintenanceRecordId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(fileObjectKey))
            throw new ArgumentException("FileObjectKey is required.", nameof(fileObjectKey));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("FileName is required.", nameof(fileName));

        TenantId = tenantId;
        MaintenanceRecordId = maintenanceRecordId;
        DocumentType = documentType;
        Title = title.Trim();
        FileObjectKey = fileObjectKey.Trim();
        FileName = fileName.Trim();
        ContentType = contentType?.Trim();
        FileSizeBytes = fileSizeBytes;
        UploadedAtUtc = uploadedAtUtc ?? DateTime.UtcNow;
        UploadedByUserId = uploadedByUserId;
        CreatedBy = uploadedByUserId;
        Notes = notes?.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid MaintenanceRecordId { get; private set; }
    public VehicleMaintenanceRecord MaintenanceRecord { get; private set; } = null!;

    public MaintenanceDocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = string.Empty;

    public string FileObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string? ContentType { get; private set; }
    public long? FileSizeBytes { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }
    public Guid? UploadedByUserId { get; private set; }
    public string? Notes { get; private set; }

    public void Update(
        MaintenanceDocumentType documentType,
        string title,
        string? notes,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        DocumentType = documentType;
        Title = title.Trim();
        Notes = notes?.Trim();
        MarkUpdated(updatedBy);
    }
}
