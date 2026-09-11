namespace ConnectedOps.Application.Fuel;

public interface IFuelAnalyticsService
{
    Task<FuelStatisticsDto> GetFleetFuelStatisticsAsync(
        Guid? branchId = null,
        Guid? vehicleId = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken cancellationToken = default);

    Task<VehicleFuelSummaryDto> GetVehicleFuelSummaryAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}
