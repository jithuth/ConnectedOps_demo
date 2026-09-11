using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Maintenance;

public interface IMaintenanceScheduleService
{
    Task<IReadOnlyCollection<VehicleMaintenanceDueDto>> CalculateVehicleMaintenanceAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleMaintenanceDueDto>> CalculateAllVehiclesMaintenanceAsync(
        Guid? branchId = null,
        Guid? vehicleCategoryId = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VehicleMaintenanceDueDto>> GetDueMaintenanceAsync(
        MaintenanceDueQueryParameters query,
        CancellationToken cancellationToken = default);
}

public interface IVehicleEngineHoursProvider
{
    Task<decimal?> GetCurrentEngineHoursAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}

public interface IMaintenanceDueEvaluationService
{
    Task EvaluateAndRefreshProjectionsAsync(
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default);
}
