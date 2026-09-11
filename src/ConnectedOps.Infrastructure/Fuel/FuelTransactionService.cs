using ConnectedOps.Application.Auditing;
using ConnectedOps.Application.Common.Exceptions;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Fuel;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Auditing;
using ConnectedOps.Domain.Fuel;
using ConnectedOps.Domain.Vehicles;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Fuel;

public sealed class FuelTransactionService : IFuelTransactionService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IVehicleOdometerService _odometerService;
    private readonly IFuelEfficiencyService _efficiencyService;
    private readonly IFuelAnomalyService _anomalyService;

    public FuelTransactionService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IVehicleOdometerService odometerService,
        IFuelEfficiencyService efficiencyService,
        IFuelAnomalyService anomalyService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _odometerService = odometerService;
        _efficiencyService = efficiencyService;
        _anomalyService = anomalyService;
    }

    public async Task<PagedResult<FuelTransactionListItemDto>> GetTransactionsPagedAsync(
        FuelTransactionQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelTransactions
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.FuelStation)
            .Include(x => x.Anomalies)
            .Include(x => x.Documents)
            .Where(x => x.TenantId == tenantId);

        if (parameters.VehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == parameters.VehicleId.Value);
        }

        if (parameters.DriverId.HasValue)
        {
            query = query.Where(x => x.DriverId == parameters.DriverId.Value);
        }

        if (parameters.FuelStationId.HasValue)
        {
            query = query.Where(x => x.FuelStationId == parameters.FuelStationId.Value);
        }

        if (parameters.FuelCardId.HasValue)
        {
            query = query.Where(x => x.FuelCardId == parameters.FuelCardId.Value);
        }

        if (parameters.BranchId.HasValue)
        {
            query = query.Where(x => x.Vehicle.BranchId == parameters.BranchId.Value);
        }

        if (parameters.FuelType.HasValue)
        {
            query = query.Where(x => x.FuelType == parameters.FuelType.Value);
        }

        if (parameters.Status.HasValue)
        {
            query = query.Where(x => x.Status == parameters.Status.Value);
        }

        if (parameters.Source.HasValue)
        {
            query = query.Where(x => x.Source == parameters.Source.Value);
        }

        if (parameters.FromUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc >= parameters.FromUtc.Value);
        }

        if (parameters.ToUtc.HasValue)
        {
            query = query.Where(x => x.TransactionDateUtc <= parameters.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            var search = parameters.SearchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Vehicle.VehicleNumber.ToLower().Contains(search) ||
                (x.Vehicle.RegistrationNumber != null && x.Vehicle.RegistrationNumber.ToLower().Contains(search)) ||
                (x.ReceiptNumber != null && x.ReceiptNumber.ToLower().Contains(search)) ||
                (x.TransactionReference != null && x.TransactionReference.ToLower().Contains(search)) ||
                (x.FuelStation != null && x.FuelStation.Name.ToLower().Contains(search)) ||
                (x.Driver != null && (x.Driver.FirstName.ToLower().Contains(search) || x.Driver.LastName.ToLower().Contains(search))));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var rawList = await query
            .OrderByDescending(x => x.TransactionDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<FuelTransactionListItemDto>();
        foreach (var x in rawList)
        {
            decimal? kml = null;
            decimal? l100 = null;
            if (x.IsFullTank && x.Status == FuelTransactionStatus.Confirmed)
            {
                var eff = await _efficiencyService.CalculateTransactionEfficiencyAsync(x.Id, cancellationToken);
                if (eff.HasSufficientData)
                {
                    kml = eff.KilometersPerLiter;
                    l100 = eff.LitersPer100Km;
                }
            }

            items.Add(new FuelTransactionListItemDto(
                x.Id,
                x.VehicleId,
                x.Vehicle.VehicleNumber,
                x.Vehicle.RegistrationNumber,
                x.Vehicle.DisplayName,
                x.DriverId,
                x.Driver != null ? $"{x.Driver.FirstName} {x.Driver.LastName}" : null,
                x.FuelStationId,
                x.FuelStation?.Name,
                x.FuelType,
                x.FuelType.ToString(),
                x.TransactionDateUtc,
                x.OdometerReading,
                x.OdometerUnit,
                x.Quantity,
                x.QuantityUnit,
                x.UnitPrice,
                x.TotalCost,
                x.CurrencyCode,
                x.IsFullTank,
                x.IsPartialFill,
                x.Status,
                x.Status.ToString(),
                x.Source,
                x.Source.ToString(),
                x.ReceiptNumber,
                x.TransactionReference,
                kml,
                l100,
                x.Anomalies.Count(a => a.Status == FuelAnomalyStatus.Open),
                x.Documents.Count,
                x.CreatedAtUtc));
        }

        return new PagedResult<FuelTransactionListItemDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<FuelTransactionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var item = await _dbContext.FuelTransactions
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Include(x => x.Driver)
            .Include(x => x.FuelStation)
            .Include(x => x.FuelCard)
            .Include(x => x.Documents)
            .Include(x => x.Anomalies)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (item is null)
            throw new KeyNotFoundException($"Fuel transaction '{id}' was not found.");

        FuelEfficiencyDto? efficiency = null;
        if (item.IsFullTank && item.Status == FuelTransactionStatus.Confirmed)
        {
            efficiency = await _efficiencyService.CalculateTransactionEfficiencyAsync(item.Id, cancellationToken);
        }

        return MapToDto(item, efficiency);
    }

    public async Task<FuelTransactionDto> CreateAsync(
        CreateFuelTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new ValidationException($"Vehicle '{request.VehicleId}' was not found.");

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers
                .AnyAsync(x => x.Id == request.DriverId.Value && x.TenantId == tenantId, cancellationToken);
            if (!driverExists)
                throw new ValidationException($"Driver '{request.DriverId.Value}' was not found.");
        }

        if (request.FuelStationId.HasValue)
        {
            var stationExists = await _dbContext.FuelStations
                .AnyAsync(x => x.Id == request.FuelStationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!stationExists)
                throw new ValidationException($"Fuel station '{request.FuelStationId.Value}' was not found.");
        }

        if (request.FuelCardId.HasValue)
        {
            var cardExists = await _dbContext.FuelCards
                .AnyAsync(x => x.Id == request.FuelCardId.Value && x.TenantId == tenantId, cancellationToken);
            if (!cardExists)
                throw new ValidationException($"Fuel card '{request.FuelCardId.Value}' was not found.");
        }

        // External transaction deduplication check
        if (!string.IsNullOrWhiteSpace(request.ExternalTransactionId))
        {
            var extExists = await _dbContext.FuelTransactions
                .AnyAsync(x => x.TenantId == tenantId &&
                               x.Source == request.Source &&
                               x.ExternalTransactionId == request.ExternalTransactionId.Trim(), cancellationToken);
            if (extExists)
                throw new ConflictException($"Transaction with external reference '{request.ExternalTransactionId}' already exists.");
        }

        // Server-side cost calculation
        var totalCost = request.TotalCost ?? Math.Round(request.Quantity * request.UnitPrice, 2);

        // Odometer validation: reading cannot be less than vehicle's current odometer
        if (request.OdometerReading.HasValue && request.OdometerReading.Value < vehicle.CurrentOdometer)
        {
            throw new ValidationException($"Odometer reading ({request.OdometerReading.Value}) cannot be less than the vehicle's current odometer reading ({vehicle.CurrentOdometer}).");
        }

        var transaction = new FuelTransaction(
            tenantId,
            vehicle.Id,
            request.TransactionDateUtc,
            request.Quantity,
            request.UnitPrice,
            totalCost,
            request.FuelType,
            request.QuantityUnit,
            request.CurrencyCode ?? "USD",
            request.IsFullTank,
            request.IsPartialFill,
            request.OdometerReading,
            request.OdometerUnit ?? vehicle.OdometerUnit,
            request.EngineHours,
            request.DriverId,
            request.FuelStationId,
            request.FuelCardId,
            request.Source,
            FuelTransactionStatus.Confirmed,
            request.TransactionReference,
            request.ReceiptNumber,
            request.PaymentMethod,
            request.ExternalTransactionId,
            request.Latitude,
            request.Longitude,
            request.Notes,
            _currentUserContext.UserId);

        _dbContext.FuelTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Phase 3 Odometer Integration
        if (request.OdometerReading.HasValue && request.OdometerReading.Value >= vehicle.CurrentOdometer)
        {
            await _odometerService.RecordOdometerAsync(
                vehicle.Id,
                new RecordVehicleOdometerRequest(
                    request.OdometerReading.Value,
                    request.OdometerUnit ?? vehicle.OdometerUnit,
                    request.TransactionDateUtc,
                    OdometerSource.Fuel,
                    $"Fuel transaction ({transaction.TransactionReference ?? transaction.ReceiptNumber ?? transaction.Id.ToString()})"),
                cancellationToken);
        }

        // Anomaly Detection Trigger
        await _anomalyService.EvaluateTransactionAnomaliesAsync(transaction.Id, cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelTransactionCreated,
                "FuelTransaction",
                transaction.Id.ToString(),
                $"Recorded fuel transaction for vehicle '{vehicle.VehicleNumber}': {transaction.Quantity} {transaction.QuantityUnit} @ {transaction.UnitPrice:F2}"),
            cancellationToken);

        return await GetByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task<FuelTransactionDto> UpdateAsync(
        Guid id,
        UpdateFuelTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var transaction = await _dbContext.FuelTransactions
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (transaction is null)
            throw new KeyNotFoundException($"Fuel transaction '{id}' was not found.");

        if (transaction.Status == FuelTransactionStatus.Cancelled)
            throw new InvalidOperationException("Cannot modify a cancelled fuel transaction.");

        var vehicle = await _dbContext.Vehicles
            .FirstOrDefaultAsync(x => x.Id == request.VehicleId && x.TenantId == tenantId, cancellationToken);

        if (vehicle is null)
            throw new ValidationException($"Vehicle '{request.VehicleId}' was not found.");

        if (request.DriverId.HasValue)
        {
            var driverExists = await _dbContext.Drivers
                .AnyAsync(x => x.Id == request.DriverId.Value && x.TenantId == tenantId, cancellationToken);
            if (!driverExists)
                throw new ValidationException($"Driver '{request.DriverId.Value}' was not found.");
        }

        if (request.FuelStationId.HasValue)
        {
            var stationExists = await _dbContext.FuelStations
                .AnyAsync(x => x.Id == request.FuelStationId.Value && x.TenantId == tenantId, cancellationToken);
            if (!stationExists)
                throw new ValidationException($"Fuel station '{request.FuelStationId.Value}' was not found.");
        }

        if (request.FuelCardId.HasValue)
        {
            var cardExists = await _dbContext.FuelCards
                .AnyAsync(x => x.Id == request.FuelCardId.Value && x.TenantId == tenantId, cancellationToken);
            if (!cardExists)
                throw new ValidationException($"Fuel card '{request.FuelCardId.Value}' was not found.");
        }

        var totalCost = request.TotalCost ?? Math.Round(request.Quantity * request.UnitPrice, 2);

        transaction.Update(
            request.VehicleId,
            request.TransactionDateUtc,
            request.Quantity,
            request.UnitPrice,
            totalCost,
            request.FuelType,
            request.QuantityUnit,
            request.CurrencyCode ?? "USD",
            request.IsFullTank,
            request.IsPartialFill,
            request.OdometerReading,
            request.OdometerUnit ?? vehicle.OdometerUnit,
            request.EngineHours,
            request.DriverId,
            request.FuelStationId,
            request.FuelCardId,
            request.TransactionReference,
            request.ReceiptNumber,
            request.PaymentMethod,
            request.Latitude,
            request.Longitude,
            request.Notes,
            _currentUserContext.UserId);

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Re-evaluate anomalies
        await _anomalyService.EvaluateTransactionAnomaliesAsync(transaction.Id, cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelTransactionUpdated,
                "FuelTransaction",
                transaction.Id.ToString(),
                $"Updated fuel transaction for vehicle '{vehicle.VehicleNumber}'"),
            cancellationToken);

        return await GetByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task<FuelTransactionDto> CancelAsync(
        Guid id,
        CancelFuelTransactionRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var transaction = await _dbContext.FuelTransactions
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (transaction is null)
            throw new KeyNotFoundException($"Fuel transaction '{id}' was not found.");

        transaction.Cancel(request?.CancellationReason, _currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelTransactionCancelled,
                "FuelTransaction",
                transaction.Id.ToString(),
                $"Cancelled fuel transaction for vehicle '{transaction.Vehicle?.VehicleNumber}'"),
            cancellationToken);

        return await GetByIdAsync(transaction.Id, cancellationToken);
    }

    public async Task<FuelTransactionDocumentDto> AddDocumentAsync(
        Guid transactionId,
        AddFuelTransactionDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var transaction = await _dbContext.FuelTransactions
            .FirstOrDefaultAsync(x => x.Id == transactionId && x.TenantId == tenantId, cancellationToken);

        if (transaction is null)
            throw new KeyNotFoundException($"Fuel transaction '{transactionId}' was not found.");

        var document = new FuelTransactionDocument(
            tenantId,
            transaction.Id,
            request.DocumentType,
            request.Title,
            request.FileObjectKey,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            DateTime.UtcNow,
            _currentUserContext.UserId,
            request.Notes);

        _dbContext.FuelTransactionDocuments.Add(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelDocumentAdded,
                "FuelTransactionDocument",
                document.Id.ToString(),
                $"Added document '{document.Title}' to fuel transaction '{transaction.Id}'"),
            cancellationToken);

        return new FuelTransactionDocumentDto(
            document.Id,
            document.FuelTransactionId,
            document.DocumentType,
            document.DocumentType.ToString(),
            document.Title,
            document.FileObjectKey,
            document.FileName,
            document.ContentType,
            document.FileSizeBytes,
            document.UploadedAtUtc,
            document.UploadedByUserId,
            null,
            document.Notes);
    }

    public async Task DeleteDocumentAsync(
        Guid transactionId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var document = await _dbContext.FuelTransactionDocuments
            .FirstOrDefaultAsync(x => x.Id == documentId && x.FuelTransactionId == transactionId && x.TenantId == tenantId, cancellationToken);

        if (document is null)
            throw new KeyNotFoundException($"Fuel transaction document '{documentId}' was not found.");

        _dbContext.FuelTransactionDocuments.Remove(document);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelDocumentRemoved,
                "FuelTransactionDocument",
                document.Id.ToString(),
                $"Removed document '{document.Title}' from fuel transaction '{transactionId}'"),
            cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static FuelTransactionDto MapToDto(FuelTransaction entity, FuelEfficiencyDto? efficiency) =>
        new(
            entity.Id,
            entity.TenantId,
            entity.VehicleId,
            entity.Vehicle?.VehicleNumber ?? string.Empty,
            entity.Vehicle?.RegistrationNumber,
            entity.Vehicle?.DisplayName ?? string.Empty,
            entity.DriverId,
            entity.Driver != null ? $"{entity.Driver.FirstName} {entity.Driver.LastName}" : null,
            entity.FuelStationId,
            entity.FuelStation?.Name,
            entity.FuelStation?.Code,
            entity.FuelCardId,
            entity.FuelCard?.CardNumberMasked,
            entity.FuelType,
            entity.FuelType.ToString(),
            entity.TransactionDateUtc,
            entity.OdometerReading,
            entity.OdometerUnit,
            entity.EngineHours,
            entity.Quantity,
            entity.QuantityUnit,
            entity.QuantityUnit.ToString(),
            entity.UnitPrice,
            entity.TotalCost,
            entity.CurrencyCode,
            entity.IsFullTank,
            entity.IsPartialFill,
            entity.Status,
            entity.Status.ToString(),
            entity.Source,
            entity.Source.ToString(),
            entity.TransactionReference,
            entity.ReceiptNumber,
            entity.PaymentMethod,
            entity.ExternalTransactionId,
            entity.Latitude,
            entity.Longitude,
            entity.Notes,
            entity.CreatedByUserId,
            null,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc,
            efficiency,
            entity.Documents.Select(d => new FuelTransactionDocumentDto(
                d.Id,
                d.FuelTransactionId,
                d.DocumentType,
                d.DocumentType.ToString(),
                d.Title,
                d.FileObjectKey,
                d.FileName,
                d.ContentType,
                d.FileSizeBytes,
                d.UploadedAtUtc,
                d.UploadedByUserId,
                null,
                d.Notes)).ToList(),
            entity.Anomalies.Select(a => new FuelAnomalyDto(
                a.Id,
                a.FuelTransactionId,
                a.VehicleId,
                entity.Vehicle?.VehicleNumber ?? string.Empty,
                entity.Vehicle?.RegistrationNumber,
                entity.Vehicle?.DisplayName ?? string.Empty,
                a.AnomalyType,
                a.AnomalyType.ToString(),
                a.Severity,
                a.Severity.ToString(),
                a.Description,
                a.DetectedAtUtc,
                a.Status,
                a.Status.ToString(),
                a.ResolvedAtUtc,
                a.ResolvedByUserId,
                null,
                a.ResolutionNotes)).ToList());
}
