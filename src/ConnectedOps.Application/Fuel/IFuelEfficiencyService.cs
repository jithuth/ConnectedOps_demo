using ConnectedOps.Domain.Fuel;

namespace ConnectedOps.Application.Fuel;

public interface IFuelEfficiencyService
{
    Task<FuelEfficiencyDto> CalculateTransactionEfficiencyAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default);

    Task<FuelEfficiencyDto> CalculateIntervalEfficiencyAsync(
        FuelTransaction currentTransaction,
        FuelTransaction? previousFullTankTransaction,
        IReadOnlyCollection<FuelTransaction> intermediatePartialTransactions,
        CancellationToken cancellationToken = default);
}
