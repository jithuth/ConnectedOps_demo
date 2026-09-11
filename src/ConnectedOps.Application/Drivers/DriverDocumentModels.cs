using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Drivers;

public sealed record DriverDocumentDto(
    Guid Id,
    Guid DriverId,
    DriverDocumentType DocumentType,
    string DocumentTypeName,
    string Title,
    string? DocumentNumber,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    string? IssuingAuthority,
    string? FileObjectKey,
    string? FileName,
    string? ContentType,
    long? FileSizeBytes,
    bool IsActive,
    bool IsExpired,
    bool IsExpiringSoon,
    string? Notes,
    DateTime CreatedAtUtc);

public sealed record CreateDriverDocumentRequest(
    DriverDocumentType DocumentType,
    string Title,
    string? DocumentNumber = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    string? IssuingAuthority = null,
    string? FileObjectKey = null,
    string? FileName = null,
    string? ContentType = null,
    long? FileSizeBytes = null,
    string? Notes = null);

public sealed record UpdateDriverDocumentRequest(
    DriverDocumentType DocumentType,
    string Title,
    string? DocumentNumber = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    string? IssuingAuthority = null,
    string? FileObjectKey = null,
    string? FileName = null,
    string? ContentType = null,
    long? FileSizeBytes = null,
    string? Notes = null);
