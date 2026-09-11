namespace ConnectedOps.Application.Telematics;

public interface IDeviceAssignmentService
{
    Task<TrackingDeviceVehicleAssignmentDto> AssignDeviceToVehicleAsync(
        AssignDeviceToVehicleRequest request,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceVehicleAssignmentDto> EndDeviceAssignmentAsync(
        Guid assignmentId,
        EndDeviceAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceVehicleAssignmentDto?> GetActiveAssignmentForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<TrackingDeviceVehicleAssignmentDto?> GetActiveAssignmentForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TrackingDeviceVehicleAssignmentDto>> GetAssignmentHistoryForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TrackingDeviceVehicleAssignmentDto>> GetAssignmentHistoryForVehicleAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}
