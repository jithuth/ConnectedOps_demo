using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Drivers;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Drivers;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Drivers;

public sealed class DriverDocumentService : IDriverDocumentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public DriverDocumentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<DriverDocumentDto>> GetDocumentsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driverExists = await _dbContext.Drivers
            .AnyAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (!driverExists)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var docs = await _dbContext.DriverDocuments
            .AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.DriverId == driverId)
            .OrderByDescending(d => d.ExpiryDate)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<DriverDocumentDto> GetDocumentByIdAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.DriverDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId && d.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Driver document '{documentId}' was not found.");

        return MapToDto(doc);
    }

    public async Task<DriverDocumentDto> AddDocumentAsync(
        Guid driverId,
        CreateDriverDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId && d.TenantId == tenantId, cancellationToken);
        if (driver is null)
            throw new KeyNotFoundException($"Driver '{driverId}' was not found.");

        var doc = new DriverDocument(
            tenantId,
            driverId,
            request.DocumentType,
            request.Title,
            request.DocumentNumber,
            request.IssueDate,
            request.ExpiryDate,
            request.IssuingAuthority,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.Notes);

        _dbContext.DriverDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "DriverDocument",
                doc.Id.ToString(),
                $"Added document {doc.Title} for driver {driver.DisplayName}"),
            cancellationToken);

        return MapToDto(doc);
    }

    public async Task<DriverDocumentDto> UpdateDocumentAsync(
        Guid driverId,
        Guid documentId,
        UpdateDriverDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.DriverDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId && d.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Driver document '{documentId}' was not found.");

        doc.Update(
            request.DocumentType,
            request.Title,
            request.DocumentNumber,
            request.IssueDate,
            request.ExpiryDate,
            request.IssuingAuthority,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "DriverDocument",
                doc.Id.ToString(),
                $"Updated document {doc.Title}"),
            cancellationToken);

        return MapToDto(doc);
    }

    public async Task DeactivateDocumentAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.DriverDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId && d.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Driver document '{documentId}' was not found.");

        doc.Deactivate();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deactivated,
                "DriverDocument",
                doc.Id.ToString(),
                $"Deactivated document {doc.Title}"),
            cancellationToken);
    }

    public async Task DeleteDocumentAsync(
        Guid driverId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.DriverDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId && d.DriverId == driverId && d.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Driver document '{documentId}' was not found.");

        doc.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "DriverDocument",
                doc.Id.ToString(),
                $"Deleted document {doc.Title}"),
            cancellationToken);
    }

    private static DriverDocumentDto MapToDto(DriverDocument d) =>
        new(
            d.Id,
            d.DriverId,
            d.DocumentType,
            d.DocumentType.ToString(),
            d.Title,
            d.DocumentNumber,
            d.IssueDate,
            d.ExpiryDate,
            d.IssuingAuthority,
            d.FileObjectKey,
            d.FileName,
            d.ContentType,
            d.FileSizeBytes,
            d.IsActive,
            d.IsExpired(),
            d.IsExpiringSoon(),
            d.Notes,
            d.CreatedAtUtc);

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");
        return tenantId;
    }
}
