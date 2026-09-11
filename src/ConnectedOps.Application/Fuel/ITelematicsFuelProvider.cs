namespace ConnectedOps.Application.Fuel;

public interface ITelematicsFuelProvider
{
    Task<decimal?> GetCurrentFuelLevelPercentAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<decimal?> GetCurrentOdometerKmAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);

    Task<decimal?> GetCurrentEngineHoursAsync(
        Guid vehicleId,
        CancellationToken cancellationToken = default);
}

public interface IFuelImportService
{
    Task<FuelImportBatchDto> ImportTransactionsAsync(
        ImportFuelTransactionsRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelImportBatchDto> GetBatchByIdAsync(
        Guid batchId,
        CancellationToken cancellationToken = default);
}
