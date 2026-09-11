using ConnectedOps.Domain.Drivers;

namespace ConnectedOps.Application.Drivers;

public sealed record DriverCertificationDto(
    Guid Id,
    Guid DriverId,
    CertificationType CertificationType,
    string CertificationTypeName,
    string Title,
    string? CertificateNumber,
    string? IssuedBy,
    DateOnly? IssueDate,
    DateOnly? ExpiryDate,
    bool IsActive,
    bool IsExpired,
    bool IsExpiringSoon,
    string? FileObjectKey,
    string? Notes);

public sealed record CreateDriverCertificationRequest(
    CertificationType CertificationType,
    string Title,
    string? CertificateNumber = null,
    string? IssuedBy = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    string? FileObjectKey = null,
    string? Notes = null);

public sealed record UpdateDriverCertificationRequest(
    CertificationType CertificationType,
    string Title,
    string? CertificateNumber = null,
    string? IssuedBy = null,
    DateOnly? IssueDate = null,
    DateOnly? ExpiryDate = null,
    string? FileObjectKey = null,
    string? Notes = null);
