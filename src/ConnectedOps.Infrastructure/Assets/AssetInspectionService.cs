using ConnectedOps.Application.Assets;
using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Assets;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Assets;

public sealed class AssetInspectionService : IAssetInspectionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetInspectionService(
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

    public async Task<PagedResult<AssetInspectionDto>> GetInspectionsPagedAsync(AssetInspectionFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.AssetInspections
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.InspectorEmployee)
            .Include(x => x.Items)
            .Where(x => x.TenantId == tenantId);

        if (filter.AssetId.HasValue) query = query.Where(x => x.AssetId == filter.AssetId.Value);
        if (filter.InspectionType.HasValue) query = query.Where(x => x.InspectionType == filter.InspectionType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.Result.HasValue) query = query.Where(x => x.Result == filter.Result.Value);
        if (filter.FromDate.HasValue) query = query.Where(x => x.InspectionDateUtc >= filter.FromDate.Value);
        if (filter.ToDate.HasValue) query = query.Where(x => x.InspectionDateUtc <= filter.ToDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.InspectionDateUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToInspectionDto).ToList();

        return new PagedResult<AssetInspectionDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<AssetInspectionDto?> GetInspectionByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var item = await _dbContext.AssetInspections
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.InspectorEmployee)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;
        return MapToInspectionDto(item);
    }

    public async Task<AssetInspectionDto> CreateInspectionAsync(CreateAssetInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        if (request.InspectorEmployeeId.HasValue)
        {
            var empExists = await _dbContext.Employees
                .AnyAsync(x => x.Id == request.InspectorEmployeeId.Value && x.TenantId == tenantId, cancellationToken);
            if (!empExists)
                throw new ValidationException($"Inspector employee '{request.InspectorEmployeeId.Value}' was not found.");
        }

        var condition = request.Result == AssetInspectionResult.Failed ? AssetCondition.NeedsAttention : AssetCondition.Good;

        var inspection = new AssetInspection(
            tenantId,
            asset.Id,
            request.InspectionType,
            request.InspectionDateUtc,
            request.InspectorEmployeeId,
            condition,
            request.Result,
            request.NextInspectionDueUtc,
            request.Remarks,
            AssetInspectionStatus.Completed,
            DateTime.UtcNow,
            _currentUserContext.UserId);

        if (request.Items != null && request.Items.Count != 0)
        {
            foreach (var item in request.Items)
            {
                var itemResult = item.IsPassed ? AssetInspectionResult.Passed : AssetInspectionResult.Failed;
                inspection.AddItem(new AssetInspectionItem(
                    tenantId,
                    inspection.Id,
                    item.CheckItemName,
                    item.Severity,
                    itemResult,
                    item.Comments));
            }
        }

        _dbContext.AssetInspections.Add(inspection);

        if (request.Result == AssetInspectionResult.Failed)
        {
            asset.SetCondition(AssetCondition.NeedsAttention, _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetInspectionCompleted,
            nameof(AssetInspection),
            inspection.Id.ToString(),
            $"Recorded {inspection.InspectionType} inspection for asset {asset.AssetNumber} (Result: {inspection.Result})",
            null,
            null), cancellationToken);

        return (await GetInspectionByIdAsync(inspection.Id, cancellationToken))!;
    }

    public async Task<AssetInspectionDto> CompleteInspectionAsync(Guid id, CompleteAssetInspectionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var inspection = await _dbContext.AssetInspections
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (inspection is null)
            throw new KeyNotFoundException($"Inspection '{id}' was not found.");

        var condition = request.Result == AssetInspectionResult.Failed ? AssetCondition.NeedsAttention : AssetCondition.Good;

        inspection.Complete(request.Result, condition, request.NextInspectionDueUtc, request.Remarks, _currentUserContext.UserId);

        if (request.Result == AssetInspectionResult.Failed)
        {
            inspection.Asset.SetCondition(AssetCondition.NeedsAttention, _currentUserContext.UserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetInspectionCompleted,
            nameof(AssetInspection),
            inspection.Id.ToString(),
            $"Completed inspection for asset {inspection.Asset.AssetNumber} (Result: {request.Result})",
            null,
            null), cancellationToken);

        return (await GetInspectionByIdAsync(inspection.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<AssetInspectionDto>> GetInspectionsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetInspections
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.InspectorEmployee)
            .Include(x => x.Items)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.InspectionDateUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToInspectionDto).ToList();
    }

    public async Task<IReadOnlyList<AssetCalibrationRecordDto>> GetCalibrationsByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetCalibrationRecords
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.CalibrationDateUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToCalibrationDto).ToList();
    }

    public async Task<AssetCalibrationRecordDto> AddCalibrationRecordAsync(CreateAssetCalibrationRecordRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var record = new AssetCalibrationRecord(
            tenantId,
            asset.Id,
            request.CalibrationDateUtc,
            request.NextCalibrationDueUtc,
            request.CertificateNumber?.Trim(),
            request.PerformedBy.Trim(),
            request.IsPassed ? "Passed" : "Failed",
            null,
            request.Remarks,
            _currentUserContext.UserId);

        _dbContext.AssetCalibrationRecords.Add(record);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetUpdated,
            nameof(AssetCalibrationRecord),
            record.Id.ToString(),
            $"Recorded calibration for asset {asset.AssetNumber} by {record.Provider}",
            null,
            null), cancellationToken);

        return MapToCalibrationDto(record);
    }

    private static AssetInspectionDto MapToInspectionDto(AssetInspection x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.InspectionType,
        x.InspectionDateUtc,
        x.InspectorEmployeeId,
        x.InspectorEmployee != null ? $"{x.InspectorEmployee.FirstName} {x.InspectorEmployee.LastName}" : null,
        x.CompletedByUserId?.ToString(),
        null,
        x.Status,
        x.Result,
        x.Notes,
        x.NextInspectionDateUtc,
        x.CreatedAtUtc,
        x.Items?.Select(i => new AssetInspectionItemDto(i.Id, i.AssetInspectionId, i.Name, i.Result == AssetInspectionResult.Passed, i.Notes, i.Description)).ToList() ?? new List<AssetInspectionItemDto>());

    private static AssetCalibrationRecordDto MapToCalibrationDto(AssetCalibrationRecord x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.CalibrationDateUtc,
        x.Provider ?? "In-House",
        x.CertificateNumber,
        x.NextCalibrationDateUtc ?? x.CalibrationDateUtc.AddYears(1),
        x.Result == "Passed",
        x.Notes,
        x.CreatedAtUtc);
}
