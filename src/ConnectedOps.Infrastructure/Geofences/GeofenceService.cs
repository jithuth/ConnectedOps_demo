using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Geofences;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Geofences;

public sealed class GeofenceService : IGeofenceService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;

    public GeofenceService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId ??
               throw new InvalidOperationException("Active tenant context is required for geofence operations.");
    }

    public async Task<IReadOnlyCollection<GeofenceListItemDto>> GetGeofencesPagedAsync(
        GeofenceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.Geofences
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Code.ToLower().Contains(search));
        }

        if (parameters.GeofenceType.HasValue)
        {
            query = query.Where(x => x.GeofenceType == parameters.GeofenceType.Value);
        }

        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == parameters.BranchId.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == parameters.IsActive.Value);
        }

        var geofences = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(x => new
            {
                Geofence = x,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                VehiclesInside = _dbContext.VehicleGeofenceStates.Count(s => s.GeofenceId == x.Id && s.IsInside)
            })
            .ToListAsync(cancellationToken);

        return geofences.Select(x => new GeofenceListItemDto(
            x.Geofence.Id,
            x.Geofence.Name,
            x.Geofence.Code,
            x.Geofence.GeofenceType,
            x.Geofence.CenterLatitude,
            x.Geofence.CenterLongitude,
            x.Geofence.RadiusMeters,
            x.Geofence.PolygonGeoJson,
            x.Geofence.BranchId,
            x.BranchName,
            x.Geofence.ColorHex,
            x.Geofence.IsActive,
            x.VehiclesInside)).ToList();
    }

    public async Task<int> GetGeofenceCountAsync(
        GeofenceQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var query = _dbContext.Geofences
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var search = parameters.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Code.ToLower().Contains(search));
        }

        if (parameters.GeofenceType.HasValue)
        {
            query = query.Where(x => x.GeofenceType == parameters.GeofenceType.Value);
        }

        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.BranchId == parameters.BranchId.Value);
        }

        if (parameters.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == parameters.IsActive.Value);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<GeofenceDto?> GetGeofenceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var item = await _dbContext.Geofences
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted)
            .Select(x => new
            {
                Geofence = x,
                BranchName = x.Branch != null ? x.Branch.Name : null,
                LocationName = x.Location != null ? x.Location.Name : null,
                VehiclesInside = _dbContext.VehicleGeofenceStates.Count(s => s.GeofenceId == x.Id && s.IsInside)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item == null)
            return null;

        return new GeofenceDto(
            item.Geofence.Id,
            item.Geofence.TenantId,
            item.Geofence.Name,
            item.Geofence.Code,
            item.Geofence.Description,
            item.Geofence.GeofenceType,
            item.Geofence.CenterLatitude,
            item.Geofence.CenterLongitude,
            item.Geofence.RadiusMeters,
            item.Geofence.PolygonGeoJson,
            item.Geofence.BranchId,
            item.BranchName,
            item.Geofence.LocationId,
            item.LocationName,
            item.Geofence.ColorHex,
            item.Geofence.IsActive,
            item.VehiclesInside,
            item.Geofence.CreatedAtUtc,
            item.Geofence.UpdatedAtUtc);
    }

    public async Task<GeofenceDto> CreateGeofenceAsync(
        CreateGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        // 1. Check code uniqueness within tenant
        var codeExists = await _dbContext.Geofences
            .AnyAsync(x => x.TenantId == tenantId && x.Code == request.Code.Trim().ToUpperInvariant() && !x.IsDeleted, cancellationToken);

        if (codeExists)
        {
            throw new InvalidOperationException($"Geofence with code '{request.Code}' already exists for this organization.");
        }

        // 2. Validate branch and location belong to tenant
        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new InvalidOperationException("Branch does not belong to active organization.");
        }

        if (request.LocationId.HasValue)
        {
            var locationExists = await _dbContext.Locations
                .AnyAsync(x => x.Id == request.LocationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!locationExists)
                throw new InvalidOperationException("Location does not belong to active organization.");
        }

        // 3. For Polygon, calculate centroid if center not provided
        double? centerLat = request.CenterLatitude;
        double? centerLon = request.CenterLongitude;

        if (request.GeofenceType == GeofenceType.Polygon && (!centerLat.HasValue || !centerLon.HasValue) && !string.IsNullOrWhiteSpace(request.PolygonGeoJson))
        {
            var vertices = GeofenceGeometryHelper.ParsePolygonVertices(request.PolygonGeoJson);
            var centroid = GeofenceGeometryHelper.CalculateCentroid(vertices);
            if (centroid.HasValue)
            {
                centerLat = centroid.Value.Latitude;
                centerLon = centroid.Value.Longitude;
            }
        }

        var geofence = new Geofence(
            tenantId: tenantId,
            name: request.Name,
            code: request.Code,
            geofenceType: request.GeofenceType,
            description: request.Description,
            centerLatitude: centerLat,
            centerLongitude: centerLon,
            radiusMeters: request.RadiusMeters,
            polygonGeoJson: request.PolygonGeoJson,
            branchId: request.BranchId,
            locationId: request.LocationId,
            colorHex: request.ColorHex,
            isActive: true);

        _dbContext.Geofences.Add(geofence);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.GeofenceCreated,
            "Geofence",
            geofence.Id.ToString(),
            $"Created geofence '{geofence.Name}' ({geofence.Code}) [{geofence.GeofenceType}]"),
            cancellationToken);

        return (await GetGeofenceByIdAsync(geofence.Id, cancellationToken))!;
    }

    public async Task<GeofenceDto> UpdateGeofenceAsync(
        Guid id,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var geofence = await _dbContext.Geofences
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken)
            ?? throw new KeyNotFoundException($"Geofence '{id}' was not found.");

        if (request.BranchId.HasValue)
        {
            var branchExists = await _dbContext.Branches
                .AnyAsync(x => x.Id == request.BranchId.Value && x.TenantId == tenantId, cancellationToken);
            if (!branchExists)
                throw new InvalidOperationException("Branch does not belong to active organization.");
        }

        if (request.LocationId.HasValue)
        {
            var locationExists = await _dbContext.Locations
                .AnyAsync(x => x.Id == request.LocationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!locationExists)
                throw new InvalidOperationException("Location does not belong to active organization.");
        }

        double? centerLat = request.CenterLatitude;
        double? centerLon = request.CenterLongitude;

        if (request.GeofenceType == GeofenceType.Polygon && (!centerLat.HasValue || !centerLon.HasValue) && !string.IsNullOrWhiteSpace(request.PolygonGeoJson))
        {
            var vertices = GeofenceGeometryHelper.ParsePolygonVertices(request.PolygonGeoJson);
            var centroid = GeofenceGeometryHelper.CalculateCentroid(vertices);
            if (centroid.HasValue)
            {
                centerLat = centroid.Value.Latitude;
                centerLon = centroid.Value.Longitude;
            }
        }

        geofence.Update(
            name: request.Name,
            geofenceType: request.GeofenceType,
            description: request.Description,
            centerLatitude: centerLat,
            centerLongitude: centerLon,
            radiusMeters: request.RadiusMeters,
            polygonGeoJson: request.PolygonGeoJson,
            branchId: request.BranchId,
            locationId: request.LocationId,
            colorHex: request.ColorHex,
            userId: _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.GeofenceUpdated,
            "Geofence",
            geofence.Id.ToString(),
            $"Updated geofence '{geofence.Name}' ({geofence.Code})"),
            cancellationToken);

        return (await GetGeofenceByIdAsync(geofence.Id, cancellationToken))!;
    }

    public async Task<bool> ActivateGeofenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var geofence = await _dbContext.Geofences
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (geofence == null) return false;

        geofence.Activate(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.GeofenceActivated,
            "Geofence",
            geofence.Id.ToString(),
            $"Activated geofence '{geofence.Name}' ({geofence.Code})"),
            cancellationToken);

        return true;
    }

    public async Task<bool> DeactivateGeofenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var geofence = await _dbContext.Geofences
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (geofence == null) return false;

        geofence.Deactivate(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.GeofenceDeactivated,
            "Geofence",
            geofence.Id.ToString(),
            $"Deactivated geofence '{geofence.Name}' ({geofence.Code})"),
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteGeofenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var geofence = await _dbContext.Geofences
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, cancellationToken);

        if (geofence == null) return false;

        geofence.MarkDeleted(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(new CreateAuditLogRequest(
            AuditAction.GeofenceDeleted,
            "Geofence",
            geofence.Id.ToString(),
            $"Deleted geofence '{geofence.Name}' ({geofence.Code})"),
            cancellationToken);

        return true;
    }

    public async Task<IReadOnlyCollection<GeofenceVehiclePresenceDto>> GetVehiclesInsideGeofenceAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var states = await _dbContext.VehicleGeofenceStates
            .AsNoTracking()
            .Where(x => x.GeofenceId == geofenceId && x.TenantId == tenantId && x.IsInside)
            .Include(x => x.Vehicle)
            .OrderByDescending(x => x.LastEnteredAtUtc)
            .ToListAsync(cancellationToken);

        var vehicleIds = states.Select(s => s.VehicleId).ToList();

        var telemetryStates = await _dbContext.VehicleTelemetryStates
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId))
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);

        var assignments = await _dbContext.DriverVehicleAssignments
            .AsNoTracking()
            .Where(x => vehicleIds.Contains(x.VehicleId) && x.IsActive)
            .Include(x => x.Driver)
            .ToDictionaryAsync(x => x.VehicleId, cancellationToken);

        return states.Select(s =>
        {
            telemetryStates.TryGetValue(s.VehicleId, out var tel);
            assignments.TryGetValue(s.VehicleId, out var assign);

            return new GeofenceVehiclePresenceDto(
                VehicleId: s.VehicleId,
                VehicleNumber: s.Vehicle.VehicleNumber,
                RegistrationNumber: s.Vehicle.RegistrationNumber,
                DisplayName: s.Vehicle.DisplayName,
                DriverId: assign?.DriverId,
                DriverName: assign?.Driver != null ? $"{assign.Driver.FirstName} {assign.Driver.LastName}".Trim() : null,
                LastEnteredAtUtc: s.LastEnteredAtUtc ?? s.LastEvaluatedAtUtc,
                LastEvaluatedAtUtc: s.LastEvaluatedAtUtc,
                Latitude: tel?.Latitude,
                Longitude: tel?.Longitude,
                SpeedKph: tel?.SpeedKph,
                IgnitionOn: tel?.IgnitionOn);
        }).ToList();
    }

    public async Task<IReadOnlyCollection<VehicleGeofenceMembershipDto>> GetGeofencesForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var memberships = await _dbContext.VehicleGeofenceStates
            .AsNoTracking()
            .Where(x => x.VehicleId == vehicleId && x.TenantId == tenantId && x.IsInside)
            .Include(x => x.Geofence)
            .OrderBy(x => x.Geofence.Name)
            .ToListAsync(cancellationToken);

        return memberships.Select(x => new VehicleGeofenceMembershipDto(
            x.GeofenceId,
            x.Geofence.Name,
            x.Geofence.Code,
            x.Geofence.GeofenceType,
            x.Geofence.ColorHex,
            x.LastEnteredAtUtc ?? x.LastEvaluatedAtUtc)).ToList();
    }

    public async Task<GeofenceEventPage> GetGeofenceEventsPagedAsync(
        GeofenceEventQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.GeofenceEvents
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (parameters.GeofenceId.HasValue)
        {
            query = query.Where(x => x.GeofenceId == parameters.GeofenceId.Value);
        }

        if (parameters.VehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == parameters.VehicleId.Value);
        }

        if (parameters.EventType.HasValue)
        {
            query = query.Where(x => x.EventType == parameters.EventType.Value);
        }

        if (parameters.FromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= parameters.FromUtc.Value);
        }

        if (parameters.ToUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= parameters.ToUtc.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        var events = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Include(x => x.Vehicle)
            .Include(x => x.Geofence)
            .Include(x => x.Driver)
            .ToListAsync(cancellationToken);

        var items = events.Select(x => new GeofenceEventDto(
            x.Id,
            x.VehicleId,
            x.Vehicle.VehicleNumber,
            x.Vehicle.DisplayName,
            x.GeofenceId,
            x.Geofence.Name,
            x.Geofence.Code,
            x.EventType,
            x.OccurredAtUtc,
            x.ReceivedAtUtc,
            x.Latitude,
            x.Longitude,
            x.DriverId,
            x.Driver != null ? $"{x.Driver.FirstName} {x.Driver.LastName}".Trim() : null,
            x.TrackingDeviceId,
            x.TelemetryRecordId)).ToList();

        return new GeofenceEventPage(items, totalCount, parameters.Page, parameters.PageSize);
    }
}
