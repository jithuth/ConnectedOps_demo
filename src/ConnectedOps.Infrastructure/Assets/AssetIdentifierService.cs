using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetIdentifierService : IAssetIdentifierService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetIdentifierService(
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

    public async Task<IReadOnlyList<AssetIdentifierDto>> GetIdentifiersByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetIdentifiers
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<AssetIdentifierDto> CreateIdentifierAsync(CreateAssetIdentifierRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var publicToken = Guid.NewGuid().ToString("N");

        var identifier = new AssetIdentifier(
            tenantId,
            asset.Id,
            request.IdentifierType,
            request.IdentifierValue.Trim(),
            publicToken,
            isActive: true);

        _dbContext.AssetIdentifiers.Add(identifier);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetIdentifierGenerated,
            nameof(AssetIdentifier),
            identifier.Id.ToString(),
            $"Assigned identifier {identifier.IdentifierType} ({identifier.Value}) to asset {asset.AssetNumber}",
            null,
            null), cancellationToken);

        return MapToDto(identifier);
    }

    public async Task<AssetIdentifierDto> GenerateQrTokenAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == assetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{assetId}' was not found.");

        var publicToken = Guid.NewGuid().ToString("N");

        var identifier = new AssetIdentifier(
            tenantId,
            asset.Id,
            AssetIdentifierType.QRCode,
            asset.AssetNumber,
            publicToken,
            isActive: true);

        _dbContext.AssetIdentifiers.Add(identifier);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetIdentifierGenerated,
            nameof(AssetIdentifier),
            identifier.Id.ToString(),
            $"Generated new QR token for asset {asset.AssetNumber}",
            null,
            null), cancellationToken);

        return MapToDto(identifier);
    }

    public async Task<AssetScanResultDto?> ScanByTokenAsync(string publicToken, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var identifier = await _dbContext.AssetIdentifiers
            .AsNoTracking()
            .Include(x => x.Asset)
                .ThenInclude(a => a.AssetCategory)
            .Include(x => x.Asset)
                .ThenInclude(a => a.AssetType)
            .Include(x => x.Asset)
                .ThenInclude(a => a.Branch)
            .Include(x => x.Asset)
                .ThenInclude(a => a.Location)
            .Include(x => x.Asset)
                .ThenInclude(a => a.CurrentCustodianEmployee)
            .Include(x => x.Asset)
                .ThenInclude(a => a.CurrentAssignedVehicle)
            .FirstOrDefaultAsync(x => x.PublicToken == publicToken && x.TenantId == tenantId && x.IsActive, cancellationToken);

        if (identifier is null) return null;

        var asset = identifier.Asset;

        return new AssetScanResultDto(
            asset.Id,
            asset.AssetNumber,
            asset.Name,
            asset.AssetCategory?.Name,
            asset.AssetType?.Name,
            asset.Status,
            asset.Condition,
            AssetAssignmentType.Permanent,
            asset.Branch?.Name,
            asset.Location?.Name,
            asset.CurrentCustodianEmployee != null ? $"{asset.CurrentCustodianEmployee.FirstName} {asset.CurrentCustodianEmployee.LastName}" : null,
            asset.CurrentAssignedVehicle != null ? (asset.CurrentAssignedVehicle.RegistrationNumber ?? asset.CurrentAssignedVehicle.VehicleNumber) : null,
            identifier.PublicToken,
            false,
            false);
    }

    public async Task DeactivateIdentifierAsync(Guid identifierId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var identifier = await _dbContext.AssetIdentifiers
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == identifierId && x.TenantId == tenantId, cancellationToken);
        if (identifier is null)
            throw new KeyNotFoundException($"Identifier '{identifierId}' was not found.");

        identifier.Deactivate(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetIdentifierDeactivated,
            nameof(AssetIdentifier),
            identifierId.ToString(),
            $"Deactivated identifier for asset {identifier.Asset.AssetNumber}",
            null,
            null), cancellationToken);
    }

    private static AssetIdentifierDto MapToDto(AssetIdentifier x) => new(
        x.Id,
        x.AssetId,
        x.IdentifierType,
        x.Value,
        x.PublicToken,
        true,
        x.IsActive,
        x.CreatedAtUtc);
}
