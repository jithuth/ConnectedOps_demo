using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelStationService : IFuelStationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public FuelStationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<PagedResult<FuelStationListItemDto>> GetStationsPagedAsync(
        FuelStationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelStations
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.TenantId == tenantId);

        if (parameters.StationType.HasValue)
        {
            query = query.Where(x => x.StationType == parameters.StationType.Value);
        }

        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == parameters.BranchId.Value);
        }

        if (parameters.ActiveOnly.HasValue && parameters.ActiveOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var search = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Code.ToLower().Contains(search) ||
                (x.VendorName != null && x.VendorName.ToLower().Contains(search)) ||
                (x.Address != null && x.Address.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .OrderBy(x => x.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FuelStationListItemDto(
                x.Id,
                x.Code,
                x.Name,
                x.VendorName,
                x.StationType,
                x.StationType.ToString(),
                x.Address,
                x.BranchId,
                x.Branch != null ? x.Branch.Name : null,
                x.ContactPhone,
                x.IsActive,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<FuelStationListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<FuelStationDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelStations
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.TenantId == tenantId);

        if (activeOnly.HasValue && activeOnly.Value)
        {
            query = query.Where(x => x.IsActive);
        }

        var list = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<FuelStationDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.FuelStations
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Fuel station '{id}' was not found.");

        return MapToDto(item);
    }

    public async Task<FuelStationDto> CreateAsync(
        CreateFuelStationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var exists = await _dbContext.FuelStations
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (exists)
            throw new ConflictException($"Fuel station with code '{normalizedCode}' already exists for this tenant.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.BranchId.Value, cancellationToken);
            if (!branchExists)
                throw new ValidationException($"Branch '{request.BranchId.Value}' was not found.");
        }

        var station = new FuelStation(
            tenantId,
            normalizedCode,
            request.Name,
            request.VendorName,
            request.StationType,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.BranchId,
            request.ContactPhone,
            request.Notes,
            request.IsActive);

        _dbContext.FuelStations.Add(station);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelStationCreated,
                "FuelStation",
                station.Id.ToString(),
                $"Created fuel station '{station.Name}' ({station.Code})"),
            cancellationToken);

        return MapToDto(station);
    }

    public async Task<FuelStationDto> UpdateAsync(
        Guid id,
        UpdateFuelStationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var station = await _dbContext.FuelStations
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (station is null)
            throw new KeyNotFoundException($"Fuel station '{id}' was not found.");

        var exists = await _dbContext.FuelStations
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (exists)
            throw new ConflictException($"Fuel station with code '{normalizedCode}' already exists for this tenant.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.TenantId == tenantId && x.Id == request.BranchId.Value, cancellationToken);
            if (!branchExists)
                throw new ValidationException($"Branch '{request.BranchId.Value}' was not found.");
        }

        station.Update(
            normalizedCode,
            request.Name,
            request.VendorName,
            request.StationType,
            request.Address,
            request.Latitude,
            request.Longitude,
            request.BranchId,
            request.ContactPhone,
            request.Notes,
            request.IsActive,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelStationUpdated,
                "FuelStation",
                station.Id.ToString(),
                $"Updated fuel station '{station.Name}' ({station.Code})"),
            cancellationToken);

        return MapToDto(station);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var station = await _dbContext.FuelStations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (station is null)
            throw new KeyNotFoundException($"Fuel station '{id}' was not found.");

        station.SoftDelete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelStationDeleted,
                "FuelStation",
                station.Id.ToString(),
                $"Deleted fuel station '{station.Name}' ({station.Code})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static FuelStationDto MapToDto(FuelStation entity) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.VendorName,
            entity.StationType,
            entity.StationType.ToString(),
            entity.Address,
            entity.Latitude,
            entity.Longitude,
            entity.BranchId,
            entity.Branch?.Name,
            entity.ContactPhone,
            entity.Notes,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
}
