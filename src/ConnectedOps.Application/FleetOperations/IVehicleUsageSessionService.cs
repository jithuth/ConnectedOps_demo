using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.FleetOperations;

public interface IVehicleUsageSessionService
{
    Task<PagedResult<VehicleUsageSessionDto>> GetSessionsPagedAsync(
        UsageSessionQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<VehicleUsageSessionDto> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<VehicleUsageSessionDto?> GetActiveSessionForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<VehicleUsageSessionDto?> GetActiveSessionForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleUsageSessionDto>> GetVehicleUsageHistoryAsync(
        Guid vehicleId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleUsageSessionDto>> GetDriverUsageHistoryAsync(
        Guid driverId,
        int limit = 50,
        CancellationToken cancellationToken = default);

    Task<VehicleUsageSessionDto> CheckoutVehicleAsync(
        CreateCheckoutRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleUsageSessionDto> CheckInVehicleAsync(
        Guid sessionId,
        CheckInSessionRequest request,
        CancellationToken cancellationToken = default);

    Task CancelSessionAsync(
        Guid sessionId,
        string? reason,
        CancellationToken cancellationToken = default);
}
