using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Maintenance;

public interface IMaintenanceRecordService
{
    Task<PagedResult<VehicleMaintenanceRecordListItemDto>> GetRecordsAsync(
        MaintenanceRecordQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<VehicleMaintenanceRecordListItemDto>> GetVehicleMaintenanceHistoryAsync(
        Guid vehicleId,
        MaintenanceRecordQueryParameters? query = null,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> CreateAsync(
        CreateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> UpdateAsync(
        Guid id,
        UpdateMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> StartServiceAsync(
        Guid id,
        StartMaintenanceRecordRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> CompleteServiceAsync(
        Guid id,
        CompleteMaintenanceRecordRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceRecordDto> CancelServiceAsync(
        Guid id,
        CancelMaintenanceRecordRequest? request = null,
        CancellationToken cancellationToken = default);

    // Tasks
    Task<VehicleMaintenanceTaskDto> AddTaskAsync(
        Guid recordId,
        AddMaintenanceTaskRequest request,
        CancellationToken cancellationToken = default);

    Task<VehicleMaintenanceTaskDto> UpdateTaskStatusAsync(
        Guid recordId,
        Guid taskId,
        UpdateMaintenanceTaskStatusRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteTaskAsync(
        Guid recordId,
        Guid taskId,
        CancellationToken cancellationToken = default);

    // Parts
    Task<VehicleMaintenancePartDto> AddPartAsync(
        Guid recordId,
        AddMaintenancePartRequest request,
        CancellationToken cancellationToken = default);

    Task DeletePartAsync(
        Guid recordId,
        Guid partId,
        CancellationToken cancellationToken = default);

    // Labour
    Task<VehicleMaintenanceLabourDto> AddLabourAsync(
        Guid recordId,
        AddMaintenanceLabourRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteLabourAsync(
        Guid recordId,
        Guid labourId,
        CancellationToken cancellationToken = default);

    // Expenses
    Task<VehicleMaintenanceExpenseDto> AddExpenseAsync(
        Guid recordId,
        AddMaintenanceExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteExpenseAsync(
        Guid recordId,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    // Documents
    Task<VehicleMaintenanceDocumentDto> AddDocumentAsync(
        Guid recordId,
        AddMaintenanceDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid recordId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
