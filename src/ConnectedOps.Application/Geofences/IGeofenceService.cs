namespace ConnectedOps.Application.Geofences;

public interface IGeofenceService
{
    Task<IReadOnlyCollection<GeofenceListItemDto>> GetGeofencesPagedAsync(
        GeofenceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<int> GetGeofenceCountAsync(
        GeofenceQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<GeofenceDto?> GetGeofenceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<GeofenceDto> CreateGeofenceAsync(
        CreateGeofenceRequest request,
        CancellationToken cancellationToken = default);

    Task<GeofenceDto> UpdateGeofenceAsync(
        Guid id,
        UpdateGeofenceRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ActivateGeofenceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateGeofenceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteGeofenceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<GeofenceVehiclePresenceDto>> GetVehiclesInsideGeofenceAsync(
        Guid geofenceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleGeofenceMembershipDto>> GetGeofencesForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<GeofenceEventPage> GetGeofenceEventsPagedAsync(
        GeofenceEventQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
