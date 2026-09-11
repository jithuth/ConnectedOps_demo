using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Organization;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Organization;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Organization;

public sealed class LocationService : ILocationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public LocationService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyCollection<LocationListItemDto>> GetLocationsAsync(
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.Locations
            .AsNoTracking()
            .Include(x => x.Branch)
            .Where(x => x.TenantId == tenantId);

        if (branchId.HasValue && branchId.Value != Guid.Empty)
        {
            query = query.Where(x => x.BranchId == branchId.Value);
        }

        var locations = await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return locations.Select(x => new LocationListItemDto(
            x.Id,
            x.BranchId,
            x.Branch?.Name ?? string.Empty,
            x.Name,
            x.Code,
            x.Type,
            x.Type.ToString(),
            x.Latitude,
            x.Longitude,
            x.City,
            x.CountryCode,
            x.IsActive)).ToList();
    }

    public async Task<LocationDto> GetLocationByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var location = await _dbContext.Locations
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (location is null)
            throw new KeyNotFoundException($"Location '{id}' was not found.");

        return MapToDto(location);
    }

    public async Task<LocationDto> CreateLocationAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var branchExists = await _dbContext.Branches
            .AnyAsync(x => x.Id == request.BranchId && x.TenantId == tenantId, cancellationToken);

        if (!branchExists)
            throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");

        var codeExists = await _dbContext.Locations
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Location code '{normalizedCode}' already exists.");

        var location = new Location(
            tenantId,
            request.BranchId,
            request.Name,
            normalizedCode,
            request.Type,
            request.Latitude,
            request.Longitude,
            request.GeofenceRadiusMeters,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode);

        _dbContext.Locations.Add(location);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Created,
                "Location",
                location.Id.ToString(),
                $"Created location: {location.Name} ({location.Code})"),
            cancellationToken);

        var created = await _dbContext.Locations
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstAsync(x => x.Id == location.Id, cancellationToken);

        return MapToDto(created);
    }

    public async Task<LocationDto> UpdateLocationAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;
        var normalizedCode = request.Code.Trim().ToUpperInvariant();

        var location = await _dbContext.Locations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (location is null)
            throw new KeyNotFoundException($"Location '{id}' was not found.");

        var branchExists = await _dbContext.Branches
            .AnyAsync(x => x.Id == request.BranchId && x.TenantId == tenantId, cancellationToken);

        if (!branchExists)
            throw new KeyNotFoundException($"Branch '{request.BranchId}' was not found in the current organization.");

        var codeExists = await _dbContext.Locations
            .AnyAsync(x => x.TenantId == tenantId && x.Code == normalizedCode && x.Id != id, cancellationToken);

        if (codeExists)
            throw new ConflictException($"Location code '{normalizedCode}' already exists.");

        location.Update(
            request.BranchId,
            request.Name,
            normalizedCode,
            request.Type,
            request.Latitude,
            request.Longitude,
            request.GeofenceRadiusMeters,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.StateOrProvince,
            request.PostalCode,
            request.CountryCode);

        location.MarkUpdated(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Updated,
                "Location",
                location.Id.ToString(),
                $"Updated location: {location.Name} ({location.Code})"),
            cancellationToken);

        var updated = await _dbContext.Locations
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstAsync(x => x.Id == location.Id, cancellationToken);

        return MapToDto(updated);
    }

    public async Task DeleteLocationAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId;

        var location = await _dbContext.Locations
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (location is null)
            throw new KeyNotFoundException($"Location '{id}' was not found.");

        location.SoftDelete(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.Deleted,
                "Location",
                location.Id.ToString(),
                $"Deleted location: {location.Name} ({location.Code})"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static LocationDto MapToDto(Location location) =>
        new(
            location.Id,
            location.TenantId,
            location.BranchId,
            location.Branch?.Name ?? string.Empty,
            location.Name,
            location.Code,
            location.Type,
            location.Type.ToString(),
            location.Latitude,
            location.Longitude,
            location.GeofenceRadiusMeters,
            location.AddressLine1,
            location.AddressLine2,
            location.City,
            location.StateOrProvince,
            location.PostalCode,
            location.CountryCode,
            location.IsActive,
            location.CreatedAtUtc,
            location.UpdatedAtUtc);
}
