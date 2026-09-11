namespace ConnectedOps.Application.Maintenance;

public interface IMaintenanceServiceTypeService
{
    Task<IReadOnlyCollection<MaintenanceServiceTypeDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceServiceTypeDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MaintenanceServiceTypeDto> CreateAsync(
        CreateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenanceServiceTypeDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceServiceTypeRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
