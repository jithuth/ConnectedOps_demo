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

public sealed class FuelAnomalyService : IFuelAnomalyService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuditLogService _auditLogService;
    private readonly IFuelEfficiencyService _efficiencyService;

    public FuelAnomalyService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAuditLogService auditLogService,
        IFuelEfficiencyService efficiencyService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _auditLogService = auditLogService;
        _efficiencyService = efficiencyService;
    }

    public async Task<PagedResult<FuelAnomalyDto>> GetAnomaliesPagedAsync(
        FuelAnomalyQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.FuelAnomalies
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Where(x => x.TenantId == tenantId);

        if (parameters.Status.HasValue)
        {
            query = query.Where(x => x.Status == parameters.Status.Value);
        }

        if (parameters.AnomalyType.HasValue)
        {
            query = query.Where(x => x.AnomalyType == parameters.AnomalyType.Value);
        }

        if (parameters.Severity.HasValue)
        {
            query = query.Where(x => x.Severity == parameters.Severity.Value);
        }

        if (parameters.VehicleId.HasValue)
        {
            query = query.Where(x => x.VehicleId == parameters.VehicleId.Value);
        }

        if (parameters.FromUtc.HasValue)
        {
            query = query.Where(x => x.DetectedAtUtc >= parameters.FromUtc.Value);
        }

        if (parameters.ToUtc.HasValue)
        {
            query = query.Where(x => x.DetectedAtUtc <= parameters.ToUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var pageNumber = Math.Max(1, parameters.PageNumber);
        var pageSize = Math.Clamp(parameters.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(x => x.DetectedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => MapToDto(x))
            .ToListAsync(cancellationToken);

        return new PagedResult<FuelAnomalyDto>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyCollection<FuelAnomalyDto>> EvaluateTransactionAnomaliesAsync(
        Guid fuelTransactionId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.FuelTransactions
            .Include(x => x.Vehicle)
                .ThenInclude(v => v.Specification)
            .FirstOrDefaultAsync(x => x.Id == fuelTransactionId, cancellationToken);

        if (transaction is null || transaction.Status == FuelTransactionStatus.Cancelled)
        {
            return Array.Empty<FuelAnomalyDto>();
        }

        var tenantId = transaction.TenantId;
        var detectedAnomalies = new List<FuelAnomaly>();

        var qtyLiters = FuelUnitConverter.ToLiters(transaction.Quantity, transaction.QuantityUnit);

        // 1. Tank Capacity Exceeded
        var tankCapacity = transaction.Vehicle?.Specification?.FuelTankCapacity;
        if (tankCapacity.HasValue && tankCapacity.Value > 0)
        {
            if (qtyLiters > tankCapacity.Value * 1.25m)
            {
                detectedAnomalies.Add(new FuelAnomaly(
                    tenantId,
                    transaction.Id,
                    transaction.VehicleId,
                    FuelAnomalyType.TankCapacityExceeded,
                    FuelAnomalySeverity.High,
                    $"Fuel quantity ({qtyLiters:F1} L) exceeds vehicle tank capacity ({tankCapacity.Value:F1} L) by more than 25%."));
            }
        }

        // 2. High Unit Price Check (against 30-day tenant average for same fuel type)
        var thirtyDaysAgo = transaction.TransactionDateUtc.AddDays(-30);
        var recentAvgPrice = await _dbContext.FuelTransactions
            .Where(x => x.TenantId == tenantId &&
                        x.FuelType == transaction.FuelType &&
                        x.Status != FuelTransactionStatus.Cancelled &&
                        x.TransactionDateUtc >= thirtyDaysAgo &&
                        x.TransactionDateUtc <= transaction.TransactionDateUtc &&
                        x.Id != transaction.Id)
            .AverageAsync(x => (decimal?)x.UnitPrice, cancellationToken);

        if (recentAvgPrice.HasValue && recentAvgPrice.Value > 0)
        {
            if (transaction.UnitPrice > recentAvgPrice.Value * 2.0m)
            {
                detectedAnomalies.Add(new FuelAnomaly(
                    tenantId,
                    transaction.Id,
                    transaction.VehicleId,
                    FuelAnomalyType.HighUnitPrice,
                    FuelAnomalySeverity.Medium,
                    $"Unit price of {transaction.UnitPrice:F2} is more than double the 30-day average of {recentAvgPrice.Value:F2}."));
            }
        }

        // 3. Rapid Repeat / Duplicate Fueling
        var windowStart = transaction.TransactionDateUtc.AddMinutes(-15);
        var windowEnd = transaction.TransactionDateUtc.AddMinutes(15);
        var exactDuplicate = await _dbContext.FuelTransactions
            .AnyAsync(x => x.TenantId == tenantId &&
                           x.VehicleId == transaction.VehicleId &&
                           x.Status != FuelTransactionStatus.Cancelled &&
                           x.Id != transaction.Id &&
                           x.Quantity == transaction.Quantity &&
                           x.TransactionDateUtc >= windowStart &&
                           x.TransactionDateUtc <= windowEnd, cancellationToken);

        if (exactDuplicate)
        {
            detectedAnomalies.Add(new FuelAnomaly(
                tenantId,
                transaction.Id,
                transaction.VehicleId,
                FuelAnomalyType.DuplicateFueling,
                FuelAnomalySeverity.High,
                "Potential duplicate fuel transaction recorded within 15 minutes with identical quantity."));
        }
        else
        {
            var twoHoursAgo = transaction.TransactionDateUtc.AddHours(-2);
            var repeatCount = await _dbContext.FuelTransactions
                .CountAsync(x => x.TenantId == tenantId &&
                                 x.VehicleId == transaction.VehicleId &&
                                 x.Status != FuelTransactionStatus.Cancelled &&
                                 x.Id != transaction.Id &&
                                 x.TransactionDateUtc >= twoHoursAgo &&
                                 x.TransactionDateUtc <= transaction.TransactionDateUtc, cancellationToken);

            if (repeatCount > 0)
            {
                detectedAnomalies.Add(new FuelAnomaly(
                    tenantId,
                    transaction.Id,
                    transaction.VehicleId,
                    FuelAnomalyType.RapidRepeatFueling,
                    FuelAnomalySeverity.Medium,
                    $"Vehicle refueled {repeatCount + 1} times within a 2-hour window."));
            }
        }

        // 4. Low Efficiency & Fuel Without Distance
        if (transaction.IsFullTank)
        {
            var efficiency = await _efficiencyService.CalculateTransactionEfficiencyAsync(transaction.Id, cancellationToken);
            if (efficiency.HasSufficientData)
            {
                if (efficiency.DistanceKilometers.HasValue && efficiency.DistanceKilometers.Value <= 0)
                {
                    detectedAnomalies.Add(new FuelAnomaly(
                        tenantId,
                        transaction.Id,
                        transaction.VehicleId,
                        FuelAnomalyType.FuelWithoutDistance,
                        FuelAnomalySeverity.Medium,
                        "Full-tank refueling recorded with 0 km distance traveled since previous fill."));
                }
                else if (efficiency.KilometersPerLiter.HasValue && efficiency.KilometersPerLiter.Value < 1.5m)
                {
                    detectedAnomalies.Add(new FuelAnomaly(
                        tenantId,
                        transaction.Id,
                        transaction.VehicleId,
                        FuelAnomalyType.LowEfficiency,
                        FuelAnomalySeverity.High,
                        $"Abnormally low efficiency calculated: {efficiency.KilometersPerLiter.Value:F2} km/L ({efficiency.LitersPer100Km:F1} L/100km)."));
                }
            }
        }

        // Persist newly discovered anomalies if not already recorded
        var existingTypes = await _dbContext.FuelAnomalies
            .Where(x => x.FuelTransactionId == transaction.Id)
            .Select(x => x.AnomalyType)
            .ToListAsync(cancellationToken);

        var toAdd = detectedAnomalies.Where(x => !existingTypes.Contains(x.AnomalyType)).ToList();

        if (toAdd.Count > 0)
        {
            _dbContext.FuelAnomalies.AddRange(toAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var allTransactionAnomalies = await _dbContext.FuelAnomalies
            .AsNoTracking()
            .Include(x => x.Vehicle)
            .Where(x => x.FuelTransactionId == transaction.Id)
            .ToListAsync(cancellationToken);

        return allTransactionAnomalies.Select(MapToDto).ToList();
    }

    public async Task<FuelAnomalyDto> ResolveAsync(
        Guid id,
        ResolveFuelAnomalyRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId ?? Guid.Empty;

        var anomaly = await _dbContext.FuelAnomalies
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (anomaly is null)
            throw new KeyNotFoundException($"Fuel anomaly '{id}' was not found.");

        anomaly.Resolve(request.ResolutionNotes, userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelAnomalyResolved,
                "FuelAnomaly",
                anomaly.Id.ToString(),
                $"Resolved fuel anomaly '{anomaly.AnomalyType}' on vehicle '{anomaly.Vehicle?.VehicleNumber}'"),
            cancellationToken);

        return MapToDto(anomaly);
    }

    public async Task<FuelAnomalyDto> DismissAsync(
        Guid id,
        DismissFuelAnomalyRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var userId = _currentUserContext.UserId ?? Guid.Empty;

        var anomaly = await _dbContext.FuelAnomalies
            .Include(x => x.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (anomaly is null)
            throw new KeyNotFoundException($"Fuel anomaly '{id}' was not found.");

        anomaly.Dismiss(request.DismissalReason, userId);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.WriteAsync(
            new CreateAuditLogRequest(
                AuditAction.FuelAnomalyDismissed,
                "FuelAnomaly",
                anomaly.Id.ToString(),
                $"Dismissed fuel anomaly '{anomaly.AnomalyType}' on vehicle '{anomaly.Vehicle?.VehicleNumber}'"),
            cancellationToken);

        return MapToDto(anomaly);
    }

    private Guid GetCurrentTenantId()
    {
        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static FuelAnomalyDto MapToDto(FuelAnomaly x) =>
        new(
            x.Id,
            x.FuelTransactionId,
            x.VehicleId,
            x.Vehicle?.VehicleNumber ?? string.Empty,
            x.Vehicle?.RegistrationNumber,
            x.Vehicle?.DisplayName ?? string.Empty,
            x.AnomalyType,
            x.AnomalyType.ToString(),
            x.Severity,
            x.Severity.ToString(),
            x.Description,
            x.DetectedAtUtc,
            x.Status,
            x.Status.ToString(),
            x.ResolvedAtUtc,
            x.ResolvedByUserId,
            null,
            x.ResolutionNotes);
}
