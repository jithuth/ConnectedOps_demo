namespace ConnectedOps.Application.Drivers;

public interface IDriverEligibilityService
{
    Task<DriverEligibilityResult> EvaluateAsync(
        Guid driverId,
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}
