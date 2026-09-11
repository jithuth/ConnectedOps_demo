using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelImportService : IFuelImportService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IFuelAnomalyService _anomalyService;

    public FuelImportService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IVehicleOdometerService odometerService,
        IFuelAnomalyService anomalyService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _odometerService = odometerService;
        _anomalyService = anomalyService;
    }

    public async Task<FuelImportBatchDto> ImportTransactionsAsync(
        ImportFuelTransactionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var rows = request.Rows ?? Array.Empty<ImportFuelTransactionRowRequest>();

        var batch = new FuelImportBatch(
            tenantId,
            request.Source,
            request.FileName,
            request.ProviderName,
            DateTime.UtcNow,
            _currentUserContext.UserId);

        batch.SetTotalRows(rows.Count);
        _dbContext.FuelImportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelImportStarted,
                "FuelImportBatch",
                batch.Id.ToString(),
                $"Started fuel import batch '{batch.FileName ?? batch.Id.ToString()}' ({rows.Count} rows)"),
            cancellationToken);

        var vehicles = await _dbContext.Vehicles
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var stations = await _dbContext.FuelStations
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var cards = await _dbContext.FuelCards
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var drivers = await _dbContext.Drivers
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            try
            {
                // Find vehicle
                var vehicleIdentifier = row.VehicleNumberOrRegistration.Trim();
                var vehicle = vehicles.FirstOrDefault(v =>
                    string.Equals(v.VehicleNumber, vehicleIdentifier, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(v.RegistrationNumber, vehicleIdentifier, StringComparison.OrdinalIgnoreCase));

                if (vehicle is null)
                {
                    var err = new FuelImportError(
                        tenantId,
                        batch.Id,
                        "VEHICLE_NOT_FOUND",
                        $"Vehicle '{vehicleIdentifier}' could not be resolved.",
                        row.RowNumber,
                        row.ExternalTransactionId);
                    batch.AddError(err);
                    _dbContext.FuelImportErrors.Add(err);
                    continue;
                }

                // Check deduplication by external ID
                if (!string.IsNullOrWhiteSpace(row.ExternalTransactionId))
                {
                    var extId = row.ExternalTransactionId.Trim();
                    var isDuplicate = await _dbContext.FuelTransactions
                        .AnyAsync(x => x.TenantId == tenantId &&
                                       x.Source == request.Source &&
                                       x.ExternalTransactionId == extId, cancellationToken);

                    if (isDuplicate)
                    {
                        var err = new FuelImportError(
                            tenantId,
                            batch.Id,
                            "DUPLICATE_TRANSACTION",
                            $"Transaction with external reference '{extId}' has already been imported.",
                            row.RowNumber,
                            row.ExternalTransactionId);
                        batch.AddError(err);
                        _dbContext.FuelImportErrors.Add(err);
                        continue;
                    }
                }

                // Resolve Station
                Guid? stationId = null;
                if (!string.IsNullOrWhiteSpace(row.StationCodeOrName))
                {
                    var sTerm = row.StationCodeOrName.Trim();
                    var station = stations.FirstOrDefault(s =>
                        string.Equals(s.Code, sTerm, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(s.Name, sTerm, StringComparison.OrdinalIgnoreCase));
                    stationId = station?.Id;
                }

                // Resolve Card
                Guid? cardId = null;
                if (!string.IsNullOrWhiteSpace(row.CardNumberOrReference))
                {
                    var cTerm = row.CardNumberOrReference.Trim();
                    var card = cards.FirstOrDefault(c =>
                        string.Equals(c.CardReference, cTerm, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(c.CardNumberMasked, cTerm, StringComparison.OrdinalIgnoreCase));
                    cardId = card?.Id;
                }

                // Resolve Driver
                Guid? driverId = null;
                if (!string.IsNullOrWhiteSpace(row.DriverNumberOrEmail))
                {
                    var dTerm = row.DriverNumberOrEmail.Trim();
                    var driver = drivers.FirstOrDefault(d =>
                        string.Equals(d.DriverNumber, dTerm, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Email, dTerm, StringComparison.OrdinalIgnoreCase));
                    driverId = driver?.Id;
                }

                var totalCost = row.TotalCost ?? Math.Round(row.Quantity * row.UnitPrice, 2);

                var transaction = new FuelTransaction(
                    tenantId,
                    vehicle.Id,
                    row.TransactionDateUtc,
                    row.Quantity,
                    row.UnitPrice,
                    totalCost,
                    row.FuelType,
                    row.QuantityUnit,
                    row.CurrencyCode ?? "USD",
                    row.IsFullTank,
                    !row.IsFullTank,
                    row.OdometerReading,
                    row.OdometerUnit ?? vehicle.OdometerUnit,
                    row.EngineHours,
                    driverId,
                    stationId,
                    cardId,
                    request.Source,
                    FuelTransactionStatus.Completed,
                    row.ReceiptNumber,
                    row.ReceiptNumber,
                    null,
                    row.ExternalTransactionId,
                    null,
                    null,
                    row.Notes,
                    _currentUserContext.UserId);

                _dbContext.FuelTransactions.Add(transaction);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Phase 3 Odometer Update
                if (row.OdometerReading.HasValue && row.OdometerReading.Value >= vehicle.CurrentOdometer)
                {
                    await _odometerService.RecordOdometerAsync(
                        vehicle.Id,
                        new RecordVehicleOdometerRequest(
                            row.OdometerReading.Value,
                            row.OdometerUnit ?? vehicle.OdometerUnit,
                            row.TransactionDateUtc,
                            OdometerSource.Fuel,
                            $"Imported fuel transaction ({transaction.ExternalTransactionId ?? transaction.Id.ToString()})"),
                        cancellationToken);
                }

                // Anomaly evaluation
                await _anomalyService.EvaluateTransactionAnomaliesAsync(transaction.Id, cancellationToken);

                batch.IncrementImported();
            }
            catch (Exception ex)
            {
                var err = new FuelImportError(
                    tenantId,
                    batch.Id,
                    "PROCESSING_ERROR",
                    ex.Message,
                    row.RowNumber,
                    row.ExternalTransactionId);
                batch.AddError(err);
                _dbContext.FuelImportErrors.Add(err);
            }
        }

        batch.Complete(_currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelImportCompleted,
                "FuelImportBatch",
                batch.Id.ToString(),
                $"Completed fuel import: {batch.ImportedRows} imported, {batch.RejectedRows} rejected"),
            cancellationToken);

        return await GetBatchByIdAsync(batch.Id, cancellationToken);
    }

    public async Task<FuelImportBatchDto> GetBatchByIdAsync(
        Guid batchId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var batch = await _dbContext.FuelImportBatches
            .AsNoTracking()
            .Include(x => x.Errors)
            .FirstOrDefaultAsync(x => x.Id == batchId && x.TenantId == tenantId, cancellationToken);

        if (batch is null)
            throw new KeyNotFoundException($"Fuel import batch '{batchId}' was not found.");

        return new FuelImportBatchDto(
            batch.Id,
            batch.Source,
            batch.Source.ToString(),
            batch.FileName,
            batch.ProviderName,
            batch.StartedAtUtc,
            batch.CompletedAtUtc,
            batch.TotalRows,
            batch.ImportedRows,
            batch.RejectedRows,
            batch.Status,
            batch.Status.ToString(),
            batch.Errors.Select(e => new FuelImportErrorDto(
                e.Id,
                e.RowNumber,
                e.ExternalReference,
                e.ErrorCode,
                e.ErrorMessage,
                e.CreatedAtUtc)).ToList());
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
