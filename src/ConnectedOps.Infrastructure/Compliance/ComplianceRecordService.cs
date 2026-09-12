using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Compliance;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Compliance;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Compliance;

public sealed class ComplianceRecordService : IComplianceRecordService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IComplianceExpiryService _expiryService;

    public ComplianceRecordService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IComplianceExpiryService expiryService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _expiryService = expiryService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<PagedResult<ComplianceRecordDto>> GetPagedAsync(ComplianceRecordFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .Where(x => x.TenantId == tenantId);

        if (filter.SubjectType.HasValue)
            query = query.Where(x => x.SubjectType == filter.SubjectType.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(x => x.VehicleId == filter.VehicleId.Value);

        if (filter.DriverId.HasValue)
            query = query.Where(x => x.DriverId == filter.DriverId.Value);

        if (filter.AssetId.HasValue)
            query = query.Where(x => x.AssetId == filter.AssetId.Value);

        if (filter.ComplianceRequirementId.HasValue)
            query = query.Where(x => x.ComplianceRequirementId == filter.ComplianceRequirementId.Value);

        if (filter.Status.HasValue)
            query = query.Where(x => x.Status == filter.Status.Value);

        var now = DateTime.UtcNow;

        if (filter.Expired == true)
            query = query.Where(x => x.ExpiryDateUtc.HasValue && x.ExpiryDateUtc.Value < now);

        if (filter.ExpiringSoon == true)
        {
            var defaultThreshold = now.AddDays(30);
            query = query.Where(x => x.ExpiryDateUtc.HasValue && x.ExpiryDateUtc.Value >= now && x.ExpiryDateUtc.Value <= defaultThreshold);
        }

        if (filter.ExpiringWithinDays.HasValue)
        {
            var threshold = now.AddDays(filter.ExpiringWithinDays.Value);
            query = query.Where(x => x.ExpiryDateUtc.HasValue && x.ExpiryDateUtc.Value >= now && x.ExpiryDateUtc.Value <= threshold);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.ComplianceRequirement.Name.ToLower().Contains(term) ||
                x.ComplianceRequirement.Code.ToLower().Contains(term) ||
                (x.ReferenceNumber != null && x.ReferenceNumber.ToLower().Contains(term)) ||
                (x.Vehicle != null && ((x.Vehicle.RegistrationNumber != null && x.Vehicle.RegistrationNumber.ToLower().Contains(term)) || (x.Vehicle.VIN != null && x.Vehicle.VIN.ToLower().Contains(term)) || x.Vehicle.VehicleNumber.ToLower().Contains(term))) ||
                (x.Driver != null && (x.Driver.FirstName.ToLower().Contains(term) || x.Driver.LastName.ToLower().Contains(term))) ||
                (x.Asset != null && (x.Asset.AssetNumber.ToLower().Contains(term) || x.Asset.Name.ToLower().Contains(term))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x, _expiryService))
            .ToListAsync(cancellationToken);

        return new PagedResult<ComplianceRecordDto>(items, totalCount, page, pageSize);
    }

    public async Task<ComplianceRecordDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return entity == null ? null : MapToDto(entity, _expiryService);
    }

    public async Task<ComplianceRecordDto> CreateAsync(CreateComplianceRecordRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var requirement = await _dbContext.ComplianceRequirements
            .FirstOrDefaultAsync(x => x.Id == request.ComplianceRequirementId && x.TenantId == tenantId, cancellationToken);

        if (requirement == null)
            throw new NotFoundException(nameof(ComplianceRequirement), request.ComplianceRequirementId.ToString());

        await ValidateSubjectTenantAsync(tenantId, request.SubjectType, request.VehicleId, request.DriverId, request.AssetId, cancellationToken);

        var entity = new ComplianceRecord(
            tenantId,
            request.ComplianceRequirementId,
            request.SubjectType,
            request.VehicleId,
            request.DriverId,
            request.AssetId,
            request.ReferenceNumber,
            request.IssueDateUtc,
            request.EffectiveFromUtc,
            request.ExpiryDateUtc,
            request.Status,
            notes: request.Notes,
            createdByUserId: _currentUserContext.UserId);

        _dbContext.ComplianceRecords.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRecordCreated,
            nameof(ComplianceRecord),
            entity.Id.ToString(),
            $"Created compliance record for requirement {requirement.Code} ({request.SubjectType})",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ComplianceRecordDto> UpdateAsync(Guid id, UpdateComplianceRecordRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRecords
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceRecord), id.ToString());

        entity.Update(
            request.ReferenceNumber,
            request.IssueDateUtc,
            request.EffectiveFromUtc,
            request.ExpiryDateUtc,
            request.Status,
            request.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRecordUpdated,
            nameof(ComplianceRecord),
            entity.Id.ToString(),
            $"Updated compliance record for requirement {entity.ComplianceRequirement.Code}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ComplianceRecordDto> VerifyAsync(Guid id, VerifyComplianceRecordRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRecords
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceRecord), id.ToString());

        var verifierId = request.VerifiedByUserId ?? _currentUserContext.UserId ?? throw new InvalidOperationException("User ID is required for verification.");
        entity.Verify(verifierId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRecordVerified,
            nameof(ComplianceRecord),
            entity.Id.ToString(),
            $"Verified compliance record for requirement {entity.ComplianceRequirement.Code} by user {verifierId}",
            null,
            entity));

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var entity = await _dbContext.ComplianceRecords
            .Include(x => x.ComplianceRequirement)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(ComplianceRecord), id.ToString());

        entity.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceRecordDeleted,
            nameof(ComplianceRecord),
            entity.Id.ToString(),
            $"Deleted compliance record for requirement {entity.ComplianceRequirement.Code}"));
    }

    public async Task<ComplianceDocumentDto> AddDocumentAsync(Guid recordId, AddComplianceDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var record = await _dbContext.ComplianceRecords
            .FirstOrDefaultAsync(x => x.Id == recordId && x.TenantId == tenantId, cancellationToken);

        if (record == null)
            throw new NotFoundException(nameof(ComplianceRecord), recordId.ToString());

        var document = new ComplianceDocument(
            tenantId,
            recordId,
            request.DocumentType,
            request.Title,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.IssueDateUtc,
            request.ExpiryDateUtc,
            DateTime.UtcNow,
            _currentUserContext.UserId,
            request.Notes);

        _dbContext.ComplianceDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceDocumentAdded,
            nameof(ComplianceDocument),
            document.Id.ToString(),
            $"Added compliance document '{document.Title}' ({document.FileName})",
            null,
            document));

        return MapDocToDto(document);
    }

    public async Task<IReadOnlyList<ComplianceDocumentDto>> GetDocumentsAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var items = await _dbContext.ComplianceDocuments
            .AsNoTracking()
            .Where(x => x.ComplianceRecordId == recordId && x.TenantId == tenantId)
            .OrderByDescending(x => x.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapDocToDto).ToList();
    }

    public async Task DeleteDocumentAsync(Guid recordId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var doc = await _dbContext.ComplianceDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.ComplianceRecordId == recordId && x.TenantId == tenantId, cancellationToken);

        if (doc == null)
            throw new NotFoundException(nameof(ComplianceDocument), documentId.ToString());

        _dbContext.ComplianceDocuments.Remove(doc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.ComplianceDocumentRemoved,
            nameof(ComplianceDocument),
            doc.Id.ToString(),
            $"Removed compliance document '{doc.Title}'"));
    }

    public async Task<IReadOnlyList<ComplianceRecordDto>> GetSubjectRecordsAsync(ComplianceSubjectType subjectType, Guid subjectId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();
        var query = _dbContext.ComplianceRecords
            .AsNoTracking()
            .Include(x => x.ComplianceRequirement)
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.Asset)
            .Include(x => x.Documents)
            .Where(x => x.TenantId == tenantId && x.SubjectType == subjectType);

        query = subjectType switch
        {
            ComplianceSubjectType.Vehicle => query.Where(x => x.VehicleId == subjectId),
            ComplianceSubjectType.Driver => query.Where(x => x.DriverId == subjectId),
            ComplianceSubjectType.Asset => query.Where(x => x.AssetId == subjectId),
            _ => query
        };

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(x => MapToDto(x, _expiryService)).ToList();
    }

    private async Task ValidateSubjectTenantAsync(
        Guid tenantId,
        ComplianceSubjectType subjectType,
        Guid? vehicleId,
        Guid? driverId,
        Guid? assetId,
        CancellationToken cancellationToken)
    {
        if (subjectType == ComplianceSubjectType.Vehicle && vehicleId.HasValue)
        {
            var vehicleExists = await _dbContext.Vehicles.AnyAsync(x => x.Id == vehicleId.Value && x.TenantId == tenantId, cancellationToken);
            if (!vehicleExists)
                throw new NotFoundException("Vehicle", vehicleId.Value.ToString());
        }
        else if (subjectType == ComplianceSubjectType.Driver && driverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers.AnyAsync(x => x.Id == driverId.Value && x.TenantId == tenantId, cancellationToken);
            if (!driverExists)
                throw new NotFoundException("Driver", driverId.Value.ToString());
        }
        else if (subjectType == ComplianceSubjectType.Asset && assetId.HasValue)
        {
            var assetExists = await _dbContext.Assets.AnyAsync(x => x.Id == assetId.Value && x.TenantId == tenantId, cancellationToken);
            if (!assetExists)
                throw new NotFoundException("Asset", assetId.Value.ToString());
        }
    }

    private static ComplianceRecordDto MapToDto(ComplianceRecord r, IComplianceExpiryService expiryService)
    {
        int? daysRemaining = r.ExpiryDateUtc.HasValue ? expiryService.CalculateDaysRemaining(r.ExpiryDateUtc) : null;
        string? vehicleName = r.Vehicle != null ? $"{r.Vehicle.RegistrationNumber ?? r.Vehicle.VehicleNumber} ({r.Vehicle.DisplayName})" : null;
        string? driverName = r.Driver != null ? r.Driver.DisplayName : null;
        string? assetName = r.Asset != null ? $"{r.Asset.AssetNumber} - {r.Asset.Name}" : null;

        return new ComplianceRecordDto(
            r.Id,
            r.TenantId,
            r.ComplianceRequirementId,
            r.ComplianceRequirement?.Code ?? string.Empty,
            r.ComplianceRequirement?.Name ?? string.Empty,
            r.ComplianceRequirement?.RequirementType ?? ComplianceRequirementType.Other,
            r.SubjectType,
            r.VehicleId,
            vehicleName,
            r.DriverId,
            driverName,
            r.AssetId,
            assetName,
            r.ReferenceNumber,
            r.IssueDateUtc,
            r.EffectiveFromUtc,
            r.ExpiryDateUtc,
            daysRemaining,
            r.Status,
            r.VerifiedAtUtc,
            r.VerifiedByUserId,
            r.Notes,
            r.CreatedAtUtc,
            r.Documents.Select(MapDocToDto).ToList());
    }

    private static ComplianceDocumentDto MapDocToDto(ComplianceDocument d) =>
        new(
            d.Id,
            d.TenantId,
            d.ComplianceRecordId,
            d.DocumentType,
            d.Title,
            d.FileObjectKey,
            d.FileName,
            d.ContentType,
            d.FileSizeBytes,
            d.IssueDateUtc,
            d.ExpiryDateUtc,
            d.UploadedAtUtc,
            d.UploadedByUserId,
            d.Notes);
}
