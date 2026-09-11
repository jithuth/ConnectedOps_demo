namespace ConnectedOps.Application.Demo;

public interface IDemoFleetSimulator
{
    Task<DemoFleetStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<DemoFleetStatusDto> CreateDemoFleetAsync(
        CreateDemoFleetRequest request,
        CancellationToken cancellationToken = default);

    Task<DemoFleetStatusDto> StartSimulationAsync(CancellationToken cancellationToken = default);

    Task<DemoFleetStatusDto> StopSimulationAsync(CancellationToken cancellationToken = default);

    Task<DemoFleetStatusDto> ResetSimulationAsync(CancellationToken cancellationToken = default);

    Task<DemoFleetStatusDto> StepSimulationAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<DemoVehicleStateDto>> GetDemoVehiclesAsync(CancellationToken cancellationToken = default);

    Task<bool> ToggleVehicleOfflineSimulationAsync(Guid vehicleId, CancellationToken cancellationToken = default);
}
