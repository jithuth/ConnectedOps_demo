namespace ConnectedOps.Application.FleetOperations;

public interface IFleetShiftService
{
    Task<IReadOnlyCollection<FleetShiftDto>> GetShiftsAsync(
        Guid? branchId = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<FleetShiftDto> GetShiftByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FleetShiftDto> CreateShiftAsync(
        CreateFleetShiftRequest request,
        CancellationToken cancellationToken = default);

    Task<FleetShiftDto> UpdateShiftAsync(
        Guid id,
        UpdateFleetShiftRequest request,
        CancellationToken cancellationToken = default);

    Task ActivateShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeactivateShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task DeleteShiftAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
