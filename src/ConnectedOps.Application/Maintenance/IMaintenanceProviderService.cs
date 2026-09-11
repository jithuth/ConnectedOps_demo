namespace ConnectedOps.Application.Maintenance;

public interface IMaintenanceProviderService
{
    Task<IReadOnlyCollection<MaintenanceProviderDto>> GetAllAsync(
        bool? activeOnly = null,
        CancellationToken cancellationToken = default);

    Task<MaintenanceProviderDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MaintenanceProviderDto> CreateAsync(
        CreateMaintenanceProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<MaintenanceProviderDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceProviderRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
