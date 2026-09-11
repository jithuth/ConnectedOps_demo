namespace ConnectedOps.Application.FleetOperations;

public interface IFleetAvailabilityService
{
    Task<VehicleAvailabilityDto> GetVehicleAvailabilityAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<DriverAvailabilityResultDto> GetDriverAvailabilityAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<FleetAvailabilitySummaryDto> GetFleetAvailabilityAsync(
        FleetAvailabilityFilter? filter = null,
        CancellationToken cancellationToken = default);
}
