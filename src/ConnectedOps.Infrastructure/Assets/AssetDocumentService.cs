using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetDocumentService : IAssetDocumentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetDocumentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid GetTenantId() =>
        _currentUserContext.TenantId ?? throw new UnauthorizedAccessException("Tenant context is required.");

    public async Task<IReadOnlyList<AssetDocumentDto>> GetDocumentsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetDocuments
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<AssetDocumentDto> AddDocumentAsync(CreateAssetDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var document = new AssetDocument(
            tenantId,
            asset.Id,
            request.DocumentType,
            request.DocumentName.Trim(),
            request.StorageKey.Trim(),
            request.DocumentName.Trim(),
            request.ContentType?.Trim() ?? "application/octet-stream",
            request.FileSizeBytes ?? 0,
            null,
            null,
            request.ExpiryDateUtc.HasValue ? DateOnly.FromDateTime(request.ExpiryDateUtc.Value) : null,
            request.Notes?.Trim(),
            true,
            _currentUserContext.UserId);

        _dbContext.AssetDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetDocumentAdded,
            nameof(AssetDocument),
            document.Id.ToString(),
            $"Uploaded document '{document.Title}' for asset {asset.AssetNumber}",
            null,
            null), cancellationToken);

        return MapToDto(document);
    }

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var document = await _dbContext.AssetDocuments
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == documentId && x.TenantId == tenantId, cancellationToken);
        if (document is null)
            throw new KeyNotFoundException($"Document '{documentId}' was not found.");

        _dbContext.AssetDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetDocumentRemoved,
            nameof(AssetDocument),
            documentId.ToString(),
            $"Deleted document '{document.Title}' for asset {document.Asset.AssetNumber}",
            null,
            null), cancellationToken);
    }

    private static AssetDocumentDto MapToDto(AssetDocument x) => new(
        x.Id,
        x.AssetId,
        x.Title,
        x.DocumentType,
        null,
        x.FileSizeBytes,
        x.ContentType,
        x.FileObjectKey,
        x.ExpiryDate.HasValue ? x.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) : null,
        x.CreatedAtUtc,
        x.CreatedBy?.ToString(),
        null,
        x.Notes);
}
