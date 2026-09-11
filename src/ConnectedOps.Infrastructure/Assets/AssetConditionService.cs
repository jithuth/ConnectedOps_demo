using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetConditionService : IAssetConditionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetConditionService(
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

    public async Task<IReadOnlyList<AssetConditionRecordDto>> GetConditionHistoryByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetConditionRecords
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<AssetConditionRecordDto> RecordConditionAsync(CreateAssetConditionRecordRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var oldCondition = asset.Condition;
        asset.SetCondition(request.Condition, _currentUserContext.UserId);

        var record = new AssetConditionRecord(
            tenantId,
            asset.Id,
            request.Condition,
            DateTime.UtcNow,
            null,
            null,
            request.Reason,
            null,
            _currentUserContext.UserId);

        _dbContext.AssetConditionRecords.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetConditionRecorded,
            nameof(AssetConditionRecord),
            record.Id.ToString(),
            $"Condition updated from {oldCondition} to {request.Condition} for asset {asset.AssetNumber}",
            oldCondition.ToString(),
            request.Condition.ToString()), cancellationToken);

        return MapToDto(record);
    }

    private static AssetConditionRecordDto MapToDto(AssetConditionRecord x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.Condition,
        x.RecordedAtUtc,
        x.RecordedByUserId?.ToString(),
        null,
        x.Description,
        null);
}
