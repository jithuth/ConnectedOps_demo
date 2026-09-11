using ConnectedOps.Application.Geofences;
using ConnectedOps.Domain.Geofences;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Geofences;

public sealed class GeofenceEvaluationService : IGeofenceEvaluationService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ILogger<GeofenceEvaluationService> _logger;

    public GeofenceEvaluationService(
        ConnectedOpsDbContext dbContext,
        ILogger<GeofenceEvaluationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task EvaluatePositionAsync(
        Guid tenantId,
        Guid vehicleId,
        double latitude,
        double longitude,
        DateTime recordedAtUtc,
        DateTime receivedAtUtc,
        Guid? trackingDeviceId = null,
        Guid? telemetryRecordId = null,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || vehicleId == Guid.Empty)
            return;

        // 1. Get all active geofences for this tenant
        var activeGeofences = await _dbContext.Geofences
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .ToListAsync(cancellationToken);

        if (activeGeofences.Count == 0)
            return;

        // 2. Get existing geofence states for this vehicle
        var existingStates = await _dbContext.VehicleGeofenceStates
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId)
            .ToDictionaryAsync(x => x.GeofenceId, cancellationToken);

        // 3. Resolve active driver if any
        var activeDriverId = await _dbContext.DriverVehicleAssignments
            .Where(x => x.TenantId == tenantId && x.VehicleId == vehicleId && x.IsActive)
            .Select(x => (Guid?)x.DriverId)
            .FirstOrDefaultAsync(cancellationToken);

        // Pre-parse polygon vertices to avoid re-parsing inside loop
        var parsedPolygonGeofences = new Dictionary<Guid, IReadOnlyList<(double Longitude, double Latitude)>>();
        foreach (var gf in activeGeofences.Where(g => g.GeofenceType == GeofenceType.Polygon))
        {
            parsedPolygonGeofences[gf.Id] = GeofenceGeometryHelper.ParsePolygonVertices(gf.PolygonGeoJson);
        }

        foreach (var geofence in activeGeofences)
        {
            bool isInside = false;

            if (geofence.GeofenceType == GeofenceType.Circle)
            {
                if (geofence.CenterLatitude.HasValue && geofence.CenterLongitude.HasValue && geofence.RadiusMeters.HasValue)
                {
                    isInside = GeofenceGeometryHelper.IsPointInCircle(
                        latitude,
                        longitude,
                        geofence.CenterLatitude.Value,
                        geofence.CenterLongitude.Value,
                        geofence.RadiusMeters.Value);
                }
            }
            else if (geofence.GeofenceType == GeofenceType.Polygon)
            {
                if (parsedPolygonGeofences.TryGetValue(geofence.Id, out var vertices) && vertices.Count >= 3)
                {
                    isInside = GeofenceGeometryHelper.IsPointInPolygon(latitude, longitude, vertices);
                }
            }

            if (!existingStates.TryGetValue(geofence.Id, out var state))
            {
                // First evaluation for this vehicle & geofence
                state = new VehicleGeofenceState(
                    tenantId: tenantId,
                    vehicleId: vehicleId,
                    geofenceId: geofence.Id,
                    isInside: isInside,
                    lastEvaluatedAtUtc: recordedAtUtc,
                    lastEnteredAtUtc: isInside ? recordedAtUtc : null,
                    lastExitedAtUtc: null);

                _dbContext.VehicleGeofenceStates.Add(state);

                // If initially inside, generate Entered event
                if (isInside)
                {
                    var enteredEvent = new GeofenceEvent(
                        tenantId: tenantId,
                        vehicleId: vehicleId,
                        geofenceId: geofence.Id,
                        eventType: GeofenceEventType.Entered,
                        occurredAtUtc: recordedAtUtc,
                        receivedAtUtc: receivedAtUtc,
                        latitude: latitude,
                        longitude: longitude,
                        trackingDeviceId: trackingDeviceId,
                        driverId: activeDriverId,
                        telemetryRecordId: telemetryRecordId);

                    _dbContext.GeofenceEvents.Add(enteredEvent);

                    _logger.LogInformation(
                        "Vehicle '{VehicleId}' entered geofence '{GeofenceName}' ({GeofenceCode}) at {RecordedAtUtc:u}",
                        vehicleId,
                        geofence.Name,
                        geofence.Code,
                        recordedAtUtc);
                }
            }
            else
            {
                bool wasInside = state.IsInside;

                if (!wasInside && isInside)
                {
                    // Transition: Outside -> Inside (Entered)
                    state.UpdateState(true, recordedAtUtc);

                    var enteredEvent = new GeofenceEvent(
                        tenantId: tenantId,
                        vehicleId: vehicleId,
                        geofenceId: geofence.Id,
                        eventType: GeofenceEventType.Entered,
                        occurredAtUtc: recordedAtUtc,
                        receivedAtUtc: receivedAtUtc,
                        latitude: latitude,
                        longitude: longitude,
                        trackingDeviceId: trackingDeviceId,
                        driverId: activeDriverId,
                        telemetryRecordId: telemetryRecordId);

                    _dbContext.GeofenceEvents.Add(enteredEvent);

                    _logger.LogInformation(
                        "Vehicle '{VehicleId}' entered geofence '{GeofenceName}' ({GeofenceCode}) at {RecordedAtUtc:u}",
                        vehicleId,
                        geofence.Name,
                        geofence.Code,
                        recordedAtUtc);
                }
                else if (wasInside && !isInside)
                {
                    // Transition: Inside -> Outside (Exited)
                    state.UpdateState(false, recordedAtUtc);

                    var exitedEvent = new GeofenceEvent(
                        tenantId: tenantId,
                        vehicleId: vehicleId,
                        geofenceId: geofence.Id,
                        eventType: GeofenceEventType.Exited,
                        occurredAtUtc: recordedAtUtc,
                        receivedAtUtc: receivedAtUtc,
                        latitude: latitude,
                        longitude: longitude,
                        trackingDeviceId: trackingDeviceId,
                        driverId: activeDriverId,
                        telemetryRecordId: telemetryRecordId);

                    _dbContext.GeofenceEvents.Add(exitedEvent);

                    _logger.LogInformation(
                        "Vehicle '{VehicleId}' exited geofence '{GeofenceName}' ({GeofenceCode}) at {RecordedAtUtc:u}",
                        vehicleId,
                        geofence.Name,
                        geofence.Code,
                        recordedAtUtc);
                }
                else
                {
                    // No state transition -> simply update evaluation timestamp (no duplicate events created)
                    state.UpdateState(wasInside, recordedAtUtc);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
