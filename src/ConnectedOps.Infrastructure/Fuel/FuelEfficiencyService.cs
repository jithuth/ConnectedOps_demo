using ConnectedOps.Application.Fuel;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelEfficiencyService : IFuelEfficiencyService
{
    private readonly ConnectedOpsDbContext _dbContext;

    public FuelEfficiencyService(ConnectedOpsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<FuelEfficiencyDto> CalculateTransactionEfficiencyAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        var current = await _dbContext.FuelTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == transactionId, cancellationToken);

        if (current is null)
        {
            return new FuelEfficiencyDto(
                false, null, null, null, null, null, null, null, null, null,
                "Transaction not found.");
        }

        if (!current.IsFullTank)
        {
            return new FuelEfficiencyDto(
                false, null, null, null, null, null, null, null, null, null,
                "Partial fill. Efficiency is calculated upon next full-tank refueling.");
        }

        // Find previous full tank transaction for this vehicle
        var previousFullTank = await _dbContext.FuelTransactions
            .AsNoTracking()
            .Where(x => x.VehicleId == current.VehicleId &&
                        x.TenantId == current.TenantId &&
                        x.Status == FuelTransactionStatus.Completed &&
                        x.IsFullTank &&
                        (x.TransactionDateUtc < current.TransactionDateUtc ||
                         (x.TransactionDateUtc == current.TransactionDateUtc && x.Id != current.Id)))
            .OrderByDescending(x => x.TransactionDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (previousFullTank is null)
        {
            return new FuelEfficiencyDto(
                false, null, null, null, null, null, null, null, null, null,
                "First full-tank refueling recorded for vehicle. Baseline established.");
        }

        // Find intermediate partial fills between previous full tank and current
        var intermediatePartials = await _dbContext.FuelTransactions
            .AsNoTracking()
            .Where(x => x.VehicleId == current.VehicleId &&
                        x.TenantId == current.TenantId &&
                        x.Status == FuelTransactionStatus.Completed &&
                        !x.IsFullTank &&
                        x.TransactionDateUtc >= previousFullTank.TransactionDateUtc &&
                        x.TransactionDateUtc <= current.TransactionDateUtc &&
                        x.Id != current.Id &&
                        x.Id != previousFullTank.Id)
            .OrderBy(x => x.TransactionDateUtc)
            .ToListAsync(cancellationToken);

        return await CalculateIntervalEfficiencyAsync(
            current,
            previousFullTank,
            intermediatePartials,
            cancellationToken);
    }

    public Task<FuelEfficiencyDto> CalculateIntervalEfficiencyAsync(
        FuelTransaction currentTransaction,
        FuelTransaction? previousFullTankTransaction,
        IReadOnlyCollection<FuelTransaction> intermediatePartialTransactions,
        CancellationToken cancellationToken = default)
    {
        if (previousFullTankTransaction is null)
        {
            return Task.FromResult(new FuelEfficiencyDto(
                false, null, null, null, null, null, null, null, null, null,
                "Previous full-tank transaction is required for interval calculation."));
        }

        if (!currentTransaction.OdometerReading.HasValue || !previousFullTankTransaction.OdometerReading.HasValue)
        {
            return Task.FromResult(new FuelEfficiencyDto(
                false, null, null, null, null, null, null, null, null, null,
                "Odometer readings are required on both transactions to calculate distance."));
        }

        var currentOdoUnit = currentTransaction.OdometerUnit ?? OdometerUnit.Kilometers;
        var prevOdoUnit = previousFullTankTransaction.OdometerUnit ?? OdometerUnit.Kilometers;

        var currentOdoKm = FuelUnitConverter.ToKilometers(currentTransaction.OdometerReading.Value, currentOdoUnit);
        var prevOdoKm = FuelUnitConverter.ToKilometers(previousFullTankTransaction.OdometerReading.Value, prevOdoUnit);

        var distanceKm = currentOdoKm - prevOdoKm;

        if (distanceKm <= 0)
        {
            return Task.FromResult(new FuelEfficiencyDto(
                false, distanceKm, null, null, null, null, null, null, null, null,
                "Distance delta must be greater than zero."));
        }

        // Volume: current full tank + any intermediate partial fills
        var currentLiters = FuelUnitConverter.ToLiters(currentTransaction.Quantity, currentTransaction.QuantityUnit);
        var partialLiters = intermediatePartialTransactions.Sum(x => FuelUnitConverter.ToLiters(x.Quantity, x.QuantityUnit));
        var totalVolumeLiters = currentLiters + partialLiters;

        var totalCost = currentTransaction.TotalCost + intermediatePartialTransactions.Sum(x => x.TotalCost);

        var distanceMiles = Math.Round(distanceKm / FuelUnitConverter.KmPerMile, 3);
        var kmPerLiter = FuelUnitConverter.CalculateKilometersPerLiter(distanceKm, totalVolumeLiters);
        var lPer100Km = FuelUnitConverter.CalculateLitersPer100Km(distanceKm, totalVolumeLiters);
        var mpgUs = FuelUnitConverter.CalculateMilesPerGallonUs(distanceKm, totalVolumeLiters);
        var mpgUk = FuelUnitConverter.CalculateMilesPerGallonUk(distanceKm, totalVolumeLiters);
        var costPerKm = FuelUnitConverter.CalculateCostPerKilometer(totalCost, distanceKm);
        var costPerMile = FuelUnitConverter.CalculateCostPerMile(totalCost, distanceKm);

        return Task.FromResult(new FuelEfficiencyDto(
            true,
            distanceKm,
            distanceMiles,
            totalVolumeLiters,
            kmPerLiter,
            lPer100Km,
            mpgUs,
            mpgUk,
            costPerKm,
            costPerMile,
            null));
    }
}
