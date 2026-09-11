using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public sealed record FuelTransactionDocumentDto(
    Guid Id,
    Guid FuelTransactionId,
    FuelDocumentType DocumentType,
    string DocumentTypeName,
    string Title,
    string FileObjectKey,
    string FileName,
    string? ContentType,
    long? FileSizeBytes,
    DateTime UploadedAtUtc,
    Guid? UploadedByUserId,
    string? UploadedByUserName,
    string? Notes);

public sealed record AddFuelTransactionDocumentRequest(
    FuelDocumentType DocumentType,
    string Title,
    string FileObjectKey,
    string FileName,
    string? ContentType = null,
    long? FileSizeBytes = null,
    string? Notes = null);
