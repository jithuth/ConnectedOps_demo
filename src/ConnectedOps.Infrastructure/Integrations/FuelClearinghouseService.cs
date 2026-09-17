using System.Text.Json;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Integrations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Integrations;

public class FuelClearinghouseService : IFuelClearinghouseService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<FuelClearinghouseService> _logger;

    public FuelClearinghouseService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<FuelClearinghouseService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for fuel clearinghouse operations.");
    }

    public async Task<FuelFeedSyncLogDto> TriggerSyncAsync(TriggerFuelSyncRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        _logger.LogInformation("Initiating fuel clearinghouse sync for Provider: {Provider} on Tenant: {TenantId}", request.Provider, tenantId);

        // Retrieve existing vehicles to simulate provider card reconciliation
        var vehicles = await _dbContext.Vehicles
            .AsNoTracking()
            .Where(v => v.TenantId == tenantId)
            .Take(10)
            .ToListAsync(cancellationToken);

        int count = request.SimulateTransactions ? 8 : 0;
        decimal totalSpend = 0m;
        decimal totalLiters = 0m;
        var details = new List<object>();
        var random = new Random();

        for (int i = 1; i <= count; i++)
        {
            var vehicle = vehicles.Count > 0 ? vehicles[i % vehicles.Count] : null;
            decimal liters = Math.Round((decimal)(random.NextDouble() * 50 + 20), 2);
            decimal pricePerLiter = 3.65m;
            decimal spend = Math.Round(liters * pricePerLiter, 2);

            totalSpend += spend;
            totalLiters += liters;

            details.Add(new
            {
                TransactionId = $"TX-{request.Provider}-{Guid.NewGuid():N}"[..18],
                CardNumber = $"****-****-****-{4100 + i}",
                PlateNumber = vehicle?.RegistrationNumber ?? "UNREGISTERED",
                Driver = vehicle != null ? "Assigned Fleet Driver" : "External Cardholder",
                Liters = liters,
                Amount = spend,
                Currency = "USD",
                Timestamp = DateTime.UtcNow.AddHours(-i * 2)
            });
        }

        var rawSummary = JsonSerializer.Serialize(new
        {
            Provider = request.Provider.ToString(),
            ReconciledTransactions = count,
            TotalSpend = totalSpend,
            TotalLiters = totalLiters,
            Transactions = details
        });

        var syncLog = new FuelFeedSyncLog(
            tenantId,
            request.Provider,
            count,
            totalSpend,
            totalLiters,
            status: "Completed",
            errorMessage: null,
            rawSummary: rawSummary
        );

        _dbContext.FuelFeedSyncLogs.Add(syncLog);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed fuel feed sync for {Provider}: {Count} transactions, ${TotalSpend} spend",
            request.Provider, count, totalSpend);

        return new FuelFeedSyncLogDto(
            syncLog.Id,
            syncLog.Provider,
            syncLog.Provider.ToString(),
            syncLog.SyncedAtUtc,
            syncLog.TransactionsCount,
            syncLog.TotalSpend,
            syncLog.TotalLiters,
            syncLog.Status,
            syncLog.ErrorMessage,
            syncLog.RawSummary);
    }

    public async Task<PagedResult<FuelFeedSyncLogDto>> GetSyncLogsPagedAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.FuelFeedSyncLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.SyncedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new FuelFeedSyncLogDto(
                x.Id,
                x.Provider,
                x.Provider.ToString(),
                x.SyncedAtUtc,
                x.TransactionsCount,
                x.TotalSpend,
                x.TotalLiters,
                x.Status,
                x.ErrorMessage,
                x.RawSummary))
            .ToListAsync(cancellationToken);

        return new PagedResult<FuelFeedSyncLogDto>(items, totalCount, page, pageSize);
    }

    public async Task<IntegrationsDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalWebhooks = await _dbContext.WebhookSubscriptions
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var activeWebhooks = await _dbContext.WebhookSubscriptions
            .CountAsync(x => x.TenantId == tenantId && x.IsEnabled, cancellationToken);

        var deliveriesToday = await _dbContext.WebhookDeliveryAttempts
            .Where(x => x.TenantId == tenantId && x.AttemptedAtUtc >= today)
            .ToListAsync(cancellationToken);

        var totalDeliveriesToday = deliveriesToday.Count;
        var successfulDeliveriesToday = deliveriesToday.Count(d => d.Success);

        var activeApiKeys = await _dbContext.TenantApiKeys
            .CountAsync(x => x.TenantId == tenantId && x.IsActive, cancellationToken);

        var totalErpBatches = await _dbContext.ErpExportBatches
            .CountAsync(x => x.TenantId == tenantId, cancellationToken);

        var totalFuelSyncsThisMonth = await _dbContext.FuelFeedSyncLogs
            .CountAsync(x => x.TenantId == tenantId && x.SyncedAtUtc >= startOfMonth, cancellationToken);

        var recentDeliveries = await _dbContext.WebhookDeliveryAttempts
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.AttemptedAtUtc)
            .Take(5)
            .Select(x => new WebhookDeliveryAttemptDto(
                x.Id,
                x.SubscriptionId,
                x.EventType,
                x.EventType.ToString(),
                x.ResponseStatusCode,
                x.DurationMs,
                x.Success,
                x.ErrorMessage,
                x.AttemptedAtUtc,
                x.PayloadJson))
            .ToListAsync(cancellationToken);

        var recentBatches = await _dbContext.ErpExportBatches
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(5)
            .Select(x => new ErpExportBatchDto(
                x.Id,
                x.BatchNumber,
                x.TargetSystem,
                x.TargetSystem.ToString(),
                x.BatchType,
                x.BatchType.ToString(),
                x.Status,
                x.Status.ToString(),
                x.PeriodStartUtc,
                x.PeriodEndUtc,
                x.RecordCount,
                x.TotalAmount,
                x.Currency,
                x.ExportedAtUtc,
                x.ExternalReference,
                x.ErrorMessage,
                x.PayloadJson))
            .ToListAsync(cancellationToken);

        var recentFuelSyncs = await _dbContext.FuelFeedSyncLogs
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.SyncedAtUtc)
            .Take(5)
            .Select(x => new FuelFeedSyncLogDto(
                x.Id,
                x.Provider,
                x.Provider.ToString(),
                x.SyncedAtUtc,
                x.TransactionsCount,
                x.TotalSpend,
                x.TotalLiters,
                x.Status,
                x.ErrorMessage,
                x.RawSummary))
            .ToListAsync(cancellationToken);

        return new IntegrationsDashboardDto(
            totalWebhooks,
            activeWebhooks,
            totalDeliveriesToday,
            successfulDeliveriesToday,
            activeApiKeys,
            totalErpBatches,
            totalFuelSyncsThisMonth,
            recentDeliveries,
            recentBatches,
            recentFuelSyncs);
    }
}
