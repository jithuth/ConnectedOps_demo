using ConnectedOps.Domain.Assets;

namespace ConnectedOps.Application.Assets;

public sealed record AssetDocumentDto(
    Guid Id,
    Guid AssetId,
    string DocumentName,
    AssetDocumentType DocumentType,
    string? FileExtension,
    long? FileSizeBytes,
    string? ContentType,
    string StorageKey,
    DateTime? ExpiryDateUtc,
    DateTime UploadedAtUtc,
    string? UploadedByUserId,
    string? UploadedByUserName,
    string? Notes);

public sealed record CreateAssetDocumentRequest(
    Guid AssetId,
    string DocumentName,
    AssetDocumentType DocumentType,
    string? FileExtension,
    long? FileSizeBytes,
    string? ContentType,
    string StorageKey,
    DateTime? ExpiryDateUtc = null,
    string? Notes = null);
