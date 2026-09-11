using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Vehicles;

public sealed class VehicleDocumentService : IVehicleDocumentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public VehicleDocumentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<VehicleDocumentDto>> GetDocumentsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicleExists = await _dbContext.Vehicles
            .AnyAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (!vehicleExists)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var docs = await _dbContext.VehicleDocuments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId)
            .OrderBy(x => x.ExpiryDate)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        return docs.Select(MapToDto).ToList();
    }

    public async Task<VehicleDocumentDto> GetDocumentByIdAsync(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.VehicleDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == documentId && x.VehicleId == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Vehicle document '{documentId}' was not found.");

        return MapToDto(doc);
    }

    public async Task<VehicleDocumentDto> AddDocumentAsync(
        Guid vehicleId,
        CreateVehicleDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(v => v.Id == vehicleId && v.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new KeyNotFoundException($"Vehicle '{vehicleId}' was not found.");

        var doc = new VehicleDocument(
            tenantId,
            vehicleId,
            request.DocumentType,
            request.Title,
            documentNumber: request.DocumentNumber,
            issueDate: request.IssueDate,
            expiryDate: request.ExpiryDate,
            issuingAuthority: request.IssuingAuthority,
            fileObjectKey: request.FileObjectKey,
            fileName: request.FileName,
            contentType: request.ContentType,
            fileSizeBytes: request.FileSizeBytes,
            notes: request.Notes);

        _dbContext.VehicleDocuments.Add(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DocumentAdded,
                "VehicleDocument",
                doc.Id.ToString(),
                $"Added document '{doc.Title}' ({doc.DocumentType}) to vehicle {vehicle.VehicleNumber}"),
            cancellationToken);

        return MapToDto(doc);
    }

    public async Task<VehicleDocumentDto> UpdateDocumentAsync(
        Guid vehicleId,
        Guid documentId,
        UpdateVehicleDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.VehicleDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.VehicleId == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Vehicle document '{documentId}' was not found.");

        doc.Update(
            request.DocumentType,
            request.Title,
            request.DocumentNumber,
            request.IssueDate,
            request.ExpiryDate,
            request.IssuingAuthority,
            request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DocumentUpdated,
                "VehicleDocument",
                doc.Id.ToString(),
                $"Updated document '{doc.Title}' for vehicle {vehicleId}"),
            cancellationToken);

        return MapToDto(doc);
    }

    public async Task DeleteDocumentAsync(
        Guid vehicleId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var doc = await _dbContext.VehicleDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.VehicleId == vehicleId && x.TenantId == tenantId, cancellationToken);

        if (doc is null)
            throw new KeyNotFoundException($"Vehicle document '{documentId}' was not found.");

        doc.SoftDelete();
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.DocumentRemoved,
                "VehicleDocument",
                doc.Id.ToString(),
                $"Deleted document '{doc.Title}' from vehicle {vehicleId}"),
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<ExpiringVehicleDocumentAlertDto>> GetExpiringDocumentsAsync(
        int daysThreshold = 30,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thresholdDate = today.AddDays(daysThreshold);

        var query = from d in _dbContext.VehicleDocuments.AsNoTracking()
                    join v in _dbContext.Vehicles.AsNoTracking() on d.VehicleId equals v.Id
                    where d.TenantId == tenantId && d.ExpiryDate != null && d.ExpiryDate <= thresholdDate
                    orderby d.ExpiryDate
                    select new
                    {
                        d.Id,
                        d.VehicleId,
                        v.VehicleNumber,
                        d.Title,
                        d.DocumentType,
                        d.ExpiryDate
                    };

        var results = await query.ToListAsync(cancellationToken);

        return results.Select(x =>
        {
            var isExpired = x.ExpiryDate.HasValue && x.ExpiryDate.Value < today;
            var daysRemaining = x.ExpiryDate.HasValue ? x.ExpiryDate.Value.DayNumber - today.DayNumber : 0;

            return new ExpiringVehicleDocumentAlertDto(
                x.Id,
                x.VehicleId,
                x.VehicleNumber,
                x.Title,
                x.DocumentType.ToString(),
                x.ExpiryDate,
                daysRemaining,
                isExpired);
        }).ToList();
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static VehicleDocumentDto MapToDto(VehicleDocument doc) =>
        new(
            doc.Id,
            doc.VehicleId,
            doc.DocumentType,
            doc.DocumentType.ToString(),
            doc.Title,
            doc.DocumentNumber,
            doc.IssuingAuthority,
            doc.IssueDate,
            doc.ExpiryDate,
            doc.FileObjectKey ?? string.Empty,
            doc.FileName ?? string.Empty,
            doc.ContentType ?? string.Empty,
            doc.FileSizeBytes,
            doc.IsExpired(),
            doc.IsExpiringSoon(),
            doc.Notes,
            doc.CreatedAtUtc);
}
