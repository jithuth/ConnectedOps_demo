namespace ConnectedOps.Application.Drivers;

public interface IDriverAssignmentService
{
    Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetDriverAssignmentsAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetVehicleAssignmentsAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DriverVehicleAssignmentDto>> GetAllActiveAssignmentsAsync(
        CancellationToken cancellationToken = default);

    Task<DriverVehicleAssignmentDto?> GetActiveAssignmentForDriverAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<DriverVehicleAssignmentDto?> GetActiveAssignmentForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<DriverVehicleAssignmentDto> CreateAssignmentAsync(
        CreateDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<DriverVehicleAssignmentDto> EndAssignmentAsync(
        Guid assignmentId,
        EndDriverVehicleAssignmentRequest request,
        CancellationToken cancellationToken = default);
}
