namespace ConnectedOps.Application.FleetOperations;

public interface IFleetShiftAssignmentService
{
    Task<IReadOnlyCollection<FleetShiftAssignmentDto>> GetAssignmentsAsync(
        DateOnly? date = null,
        Guid? shiftId = null,
        Guid? driverId = null,
        Guid? vehicleId = null,
        CancellationToken cancellationToken = default);

    Task<FleetShiftAssignmentDto> GetAssignmentByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FleetShiftAssignmentDto> CreateAssignmentAsync(
        CreateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<FleetShiftAssignmentDto> UpdateAssignmentAsync(
        Guid id,
        UpdateShiftAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task CompleteAssignmentAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task CancelAssignmentAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);
}
