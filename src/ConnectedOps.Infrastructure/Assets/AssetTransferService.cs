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

public sealed class AssetTransferService : IAssetTransferService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public AssetTransferService(
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

    public async Task<PagedResult<AssetTransferDto>> GetPagedAsync(AssetTransferFilter filter, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var query = _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Include(x => x.FromEmployee)
            .Include(x => x.ToEmployee)
            .Include(x => x.FromVehicle)
            .Include(x => x.ToVehicle)
            .Where(x => x.TenantId == tenantId);

        if (filter.AssetId.HasValue) query = query.Where(x => x.AssetId == filter.AssetId.Value);
        if (filter.TransferType.HasValue) query = query.Where(x => x.TransferType == filter.TransferType.Value);
        if (filter.Status.HasValue) query = query.Where(x => x.Status == filter.Status.Value);
        if (filter.BranchId.HasValue) query = query.Where(x => x.FromBranchId == filter.BranchId.Value || x.ToBranchId == filter.BranchId.Value);
        if (filter.EmployeeId.HasValue) query = query.Where(x => x.FromEmployeeId == filter.EmployeeId.Value || x.ToEmployeeId == filter.EmployeeId.Value);
        if (filter.VehicleId.HasValue) query = query.Where(x => x.FromVehicleId == filter.VehicleId.Value || x.ToVehicleId == filter.VehicleId.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, filter.PageNumber);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.RequestedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(MapToDto).ToList();

        return new PagedResult<AssetTransferDto>(dtos, totalCount, pageNumber, pageSize);
    }

    public async Task<AssetTransferDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var item = await _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Include(x => x.FromEmployee)
            .Include(x => x.ToEmployee)
            .Include(x => x.FromVehicle)
            .Include(x => x.ToVehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null) return null;
        return MapToDto(item);
    }

    public async Task<AssetTransferDto> CreateTransferAsync(CreateAssetTransferRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var asset = await _dbContext.Assets
            .FirstOrDefaultAsync(x => x.Id == request.AssetId && x.TenantId == tenantId, cancellationToken);
        if (asset is null)
            throw new KeyNotFoundException($"Asset '{request.AssetId}' was not found.");

        var hasPending = await _dbContext.AssetTransfers
            .AnyAsync(x => x.AssetId == request.AssetId && x.TenantId == tenantId && (x.Status == AssetTransferStatus.Pending || x.Status == AssetTransferStatus.InTransit), cancellationToken);
        if (hasPending)
            throw new ConflictException("Asset already has a transfer in progress.");

        var transfer = new AssetTransfer(
            tenantId,
            asset.Id,
            request.TransferType,
            fromBranchId: asset.BranchId,
            toBranchId: request.TargetBranchId,
            fromLocationId: asset.LocationId,
            toLocationId: request.TargetLocationId,
            fromEmployeeId: asset.CurrentCustodianEmployeeId,
            toEmployeeId: request.TargetEmployeeId,
            fromVehicleId: asset.CurrentAssignedVehicleId,
            toVehicleId: request.TargetVehicleId,
            reason: request.Reason,
            notes: request.Notes,
            requestedAtUtc: DateTime.UtcNow,
            requestedByUserId: _currentUserContext.UserId);

        _dbContext.AssetTransfers.Add(transfer);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTransferCreated,
            nameof(AssetTransfer),
            transfer.Id.ToString(),
            $"Initiated transfer of type {transfer.TransferType} for asset {asset.AssetNumber}",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(transfer.Id, cancellationToken))!;
    }

    public async Task<AssetTransferDto> CompleteTransferAsync(Guid id, CompleteAssetTransferRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var transfer = await _dbContext.AssetTransfers
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (transfer is null)
            throw new KeyNotFoundException($"Transfer '{id}' was not found.");

        if (transfer.Status != AssetTransferStatus.Pending && transfer.Status != AssetTransferStatus.InTransit)
            throw new ValidationException("Only pending or in-transit transfers can be completed.");

        var asset = transfer.Asset;

        if (transfer.ToBranchId.HasValue || transfer.ToLocationId.HasValue)
        {
            asset.AssignLocation(transfer.ToBranchId ?? asset.BranchId, transfer.ToLocationId ?? asset.LocationId, "Transfer Completed", _currentUserContext.UserId);

            var locHistory = new AssetLocationHistory(
                tenantId,
                asset.Id,
                asset.BranchId,
                asset.LocationId,
                DateTime.UtcNow,
                null,
                $"Transfer Completed ({transfer.TransferType})",
                _currentUserContext.UserId);
            _dbContext.AssetLocationHistories.Add(locHistory);
        }

        if (transfer.ToEmployeeId.HasValue)
        {
            var oldEmpAssignments = await _dbContext.AssetEmployeeAssignments
                .Where(x => x.AssetId == asset.Id && x.TenantId == tenantId && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var old in oldEmpAssignments)
            {
                old.EndAssignment(DateTime.UtcNow, asset.Condition, "Ended via asset transfer", _currentUserContext.UserId);
            }

            var newAssignment = new AssetEmployeeAssignment(
                tenantId,
                asset.Id,
                transfer.ToEmployeeId.Value,
                DateTime.UtcNow,
                null,
                AssetAssignmentType.Permanent,
                asset.Condition,
                "Assigned via completed transfer",
                true,
                _currentUserContext.UserId);
            _dbContext.AssetEmployeeAssignments.Add(newAssignment);

            asset.SetCustodian(transfer.ToEmployeeId.Value, _currentUserContext.UserId);
        }

        if (transfer.ToVehicleId.HasValue)
        {
            var oldVehAssignments = await _dbContext.AssetVehicleAssignments
                .Where(x => x.AssetId == asset.Id && x.TenantId == tenantId && x.IsActive)
                .ToListAsync(cancellationToken);
            foreach (var old in oldVehAssignments)
            {
                old.EndAssignment(DateTime.UtcNow, "Ended via asset transfer", _currentUserContext.UserId);
            }

            var newAssignment = new AssetVehicleAssignment(
                tenantId,
                asset.Id,
                transfer.ToVehicleId.Value,
                DateTime.UtcNow,
                null,
                "Assigned via completed transfer",
                true,
                _currentUserContext.UserId);
            _dbContext.AssetVehicleAssignments.Add(newAssignment);

            asset.SetAssignedVehicle(transfer.ToVehicleId.Value, _currentUserContext.UserId);
        }

        transfer.Complete(request.Notes, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTransferCompleted,
            nameof(AssetTransfer),
            transfer.Id.ToString(),
            $"Completed transfer for asset {asset.AssetNumber}",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(transfer.Id, cancellationToken))!;
    }

    public async Task<AssetTransferDto> CancelTransferAsync(Guid id, CancelAssetTransferRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var transfer = await _dbContext.AssetTransfers
            .Include(x => x.Asset)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);
        if (transfer is null)
            throw new KeyNotFoundException($"Transfer '{id}' was not found.");

        if (transfer.Status != AssetTransferStatus.Pending && transfer.Status != AssetTransferStatus.InTransit)
            throw new ValidationException("Only pending or in-transit transfers can be cancelled.");

        transfer.Cancel(request.Reason, _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.AssetTransferCancelled,
            nameof(AssetTransfer),
            transfer.Id.ToString(),
            $"Cancelled transfer for asset {transfer.Asset.AssetNumber}. Reason: {request.Reason}",
            null,
            null), cancellationToken);

        return (await GetByIdAsync(transfer.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<AssetTransferDto>> GetTransfersByAssetAsync(Guid assetId, CancellationToken cancellationToken = default)
    {
        var tenantId = GetTenantId();

        var list = await _dbContext.AssetTransfers
            .AsNoTracking()
            .Include(x => x.Asset)
            .Include(x => x.FromBranch)
            .Include(x => x.ToBranch)
            .Include(x => x.FromLocation)
            .Include(x => x.ToLocation)
            .Include(x => x.FromEmployee)
            .Include(x => x.ToEmployee)
            .Include(x => x.FromVehicle)
            .Include(x => x.ToVehicle)
            .Where(x => x.TenantId == tenantId && x.AssetId == assetId)
            .OrderByDescending(x => x.RequestedAtUtc)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    private static AssetTransferDto MapToDto(AssetTransfer x) => new(
        x.Id,
        x.AssetId,
        x.Asset?.AssetNumber,
        x.Asset?.Name,
        x.TransferType,
        x.Status,
        x.FromBranchId,
        x.FromBranch?.Name,
        x.ToBranchId,
        x.ToBranch?.Name,
        x.FromLocationId,
        x.FromLocation?.Name,
        x.ToLocationId,
        x.ToLocation?.Name,
        x.FromEmployeeId,
        x.FromEmployee != null ? $"{x.FromEmployee.FirstName} {x.FromEmployee.LastName}" : null,
        x.ToEmployeeId,
        x.ToEmployee != null ? $"{x.ToEmployee.FirstName} {x.ToEmployee.LastName}" : null,
        x.FromVehicleId,
        x.FromVehicle != null ? (x.FromVehicle.RegistrationNumber ?? x.FromVehicle.VehicleNumber) : null,
        x.ToVehicleId,
        x.ToVehicle != null ? (x.ToVehicle.RegistrationNumber ?? x.ToVehicle.VehicleNumber) : null,
        x.RequestedAtUtc,
        x.RequestedByUserId?.ToString(),
        null,
        x.CompletedAtUtc,
        x.CompletedByUserId?.ToString(),
        null,
        x.Reason,
        x.Notes);
}
