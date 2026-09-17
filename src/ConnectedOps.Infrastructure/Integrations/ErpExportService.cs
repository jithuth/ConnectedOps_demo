using System.Text.Json;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Integrations;
using ConnectedOps.Application.Vehicles;
using ConnectedOps.Domain.Expenses;
using ConnectedOps.Domain.Integrations;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Integrations;

public sealed class ErpExportService : IErpExportService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ILogger<ErpExportService> _logger;

    public ErpExportService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ILogger<ErpExportService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _logger = logger;
    }

    private Guid RequireTenantId()
    {
        return _currentUserContext.TenantId
            ?? throw new InvalidOperationException("Tenant context is required for ERP export operations.");
    }

    public async Task<ErpExportBatchDto> GenerateBatchAsync(GenerateErpBatchRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();
        var startUtc = request.PeriodStartUtc;
        var endUtc = request.PeriodEndUtc;

        var batchNumber = $"ERP-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
        var recordCount = 0;
        var totalAmount = 0m;
        var currency = "AED";
        string payloadJson;

        if (request.BatchType == ErpBatchType.Expenses)
        {
            var expenses = await _dbContext.DriverTripExpenses
                .AsNoTracking()
                .Include(e => e.Driver)
                .Include(e => e.Vehicle)
                .Where(e => e.TenantId == tenantId && e.IncurredAtUtc >= startUtc && e.IncurredAtUtc <= endUtc && e.Status == ExpenseStatus.Approved)
                .ToListAsync(cancellationToken);

            recordCount = expenses.Count;
            totalAmount = expenses.Sum(e => e.Amount);
            if (expenses.Count > 0) currency = expenses[0].Currency;

            var items = expenses.Select(e => new
            {
                ExpenseId = e.Id,
                Date = e.IncurredAtUtc.ToString("yyyy-MM-dd"),
                Driver = e.Driver != null ? $"{e.Driver.FirstName} {e.Driver.LastName}".Trim() : "Driver",
                VehiclePlate = e.Vehicle?.RegistrationNumber ?? "N/A",
                Category = e.Category.ToString(),
                Amount = e.Amount,
                Currency = e.Currency,
                Description = e.Description,
                AccountCode = e.Category switch
                {
                    ExpenseCategory.Toll => "6010-TOLLS",
                    ExpenseCategory.Parking => "6020-PARKING",
                    ExpenseCategory.FuelEmergency => "5010-FUEL",
                    ExpenseCategory.TireRepair => "5020-MAINT",
                    _ => "6090-MISC-FLEET"
                }
            }).ToList();

            payloadJson = FormatPayload(request.TargetSystem, batchNumber, "Driver Trip Expenses", items, totalAmount, currency);
        }
        else if (request.BatchType == ErpBatchType.MaintenanceCosts)
        {
            var records = await _dbContext.VehicleMaintenanceRecords
                .AsNoTracking()
                .Include(r => r.Vehicle)
                .Where(r => r.TenantId == tenantId && r.ServiceDateUtc >= startUtc && r.ServiceDateUtc <= endUtc)
                .ToListAsync(cancellationToken);

            recordCount = records.Count;
            totalAmount = records.Sum(r => r.TotalCost ?? 0m);

            var items = records.Select(r => new
            {
                RecordId = r.Id,
                Date = r.ServiceDateUtc.ToString("yyyy-MM-dd"),
                Vehicle = r.Vehicle?.VehicleNumber ?? "N/A",
                TotalCost = r.TotalCost ?? 0m,
                AccountCode = "5020-MAINTENANCE-EXPENSE"
            }).ToList();

            payloadJson = FormatPayload(request.TargetSystem, batchNumber, "Vehicle Maintenance Costs", items, totalAmount, currency);
        }
        else if (request.BatchType == ErpBatchType.FuelSpend)
        {
            var fuelTxs = await _dbContext.FuelTransactions
                .AsNoTracking()
                .Include(f => f.Vehicle)
                .Where(f => f.TenantId == tenantId && f.TransactionDateUtc >= startUtc && f.TransactionDateUtc <= endUtc)
                .ToListAsync(cancellationToken);

            recordCount = fuelTxs.Count;
            totalAmount = fuelTxs.Sum(f => f.TotalCost);
            if (fuelTxs.Count > 0) currency = fuelTxs[0].CurrencyCode;

            var items = fuelTxs.Select(f => new
            {
                TxId = f.Id,
                Date = f.TransactionDateUtc.ToString("yyyy-MM-dd"),
                Vehicle = f.Vehicle?.VehicleNumber ?? "N/A",
                Volume = f.Quantity,
                Amount = f.TotalCost,
                AccountCode = "5010-FUEL-EXPENSE"
            }).ToList();

            payloadJson = FormatPayload(request.TargetSystem, batchNumber, "Fleet Fuel Transactions", items, totalAmount, currency);
        }
        else
        {
            // General Ledger summary
            var entries = await _dbContext.LedgerEntries
                .AsNoTracking()
                .Where(l => l.TenantId == tenantId && l.EntryDateUtc >= startUtc && l.EntryDateUtc <= endUtc)
                .ToListAsync(cancellationToken);

            recordCount = entries.Count;
            totalAmount = entries.Sum(l => l.DebitAmount > 0 ? l.DebitAmount : l.CreditAmount);

            var items = entries.Select(l => new
            {
                EntryId = l.Id,
                Date = l.EntryDateUtc.ToString("yyyy-MM-dd"),
                Amount = l.DebitAmount > 0 ? l.DebitAmount : l.CreditAmount,
                EntryType = l.DebitAmount > 0 ? "Debit" : "Credit",
                Description = l.Description
            }).ToList();

            payloadJson = FormatPayload(request.TargetSystem, batchNumber, "General Ledger Entries", items, totalAmount, currency);
        }

        var batch = new ErpExportBatch(
            tenantId,
            batchNumber,
            request.TargetSystem,
            request.BatchType,
            startUtc,
            endUtc,
            recordCount,
            totalAmount,
            payloadJson,
            currency,
            _currentUserContext.UserId);

        _dbContext.ErpExportBatches.Add(batch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated ERP export batch {BatchNumber} ({TargetSystem}) with {Count} records", batch.BatchNumber, batch.TargetSystem, recordCount);

        return MapToDto(batch);
    }

    public async Task<PagedResult<ErpExportBatchDto>> GetBatchesPagedAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var query = _dbContext.ErpExportBatches
            .AsNoTracking()
            .Where(b => b.TenantId == tenantId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => MapToDto(b))
            .ToListAsync(cancellationToken);

        return new PagedResult<ErpExportBatchDto>(items, total, page, pageSize);
    }

    public async Task<ErpExportBatchDto?> GetBatchByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var batch = await _dbContext.ErpExportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, cancellationToken);

        return batch != null ? MapToDto(batch) : null;
    }

    public async Task<bool> MarkBatchExportedAsync(Guid id, string? externalRef = null, CancellationToken cancellationToken = default)
    {
        var tenantId = RequireTenantId();

        var batch = await _dbContext.ErpExportBatches
            .FirstOrDefaultAsync(b => b.TenantId == tenantId && b.Id == id, cancellationToken);

        if (batch == null) return false;

        batch.MarkExported(externalRef, _currentUserContext.UserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string FormatPayload(ErpTargetSystem target, string batchNumber, string title, object items, decimal totalAmount, string currency)
    {
        var envelope = new
        {
            BatchMetadata = new
            {
                BatchNumber = batchNumber,
                TargetSystem = target.ToString(),
                Title = title,
                ExportGeneratedUtc = DateTime.UtcNow,
                TotalRecords = items is System.Collections.ICollection c ? c.Count : 0,
                TotalAmount = totalAmount,
                Currency = currency
            },
            LineItems = items
        };

        return JsonSerializer.Serialize(envelope, new JsonSerializerOptions { WriteIndented = true });
    }

    private static ErpExportBatchDto MapToDto(ErpExportBatch b)
    {
        return new ErpExportBatchDto(
            b.Id,
            b.BatchNumber,
            b.TargetSystem,
            b.TargetSystem.ToString(),
            b.BatchType,
            b.BatchType.ToString(),
            b.Status,
            b.Status.ToString(),
            b.PeriodStartUtc,
            b.PeriodEndUtc,
            b.RecordCount,
            b.TotalAmount,
            b.Currency,
            b.ExportedAtUtc,
            b.ExternalReference,
            b.ErrorMessage,
            b.PayloadJson);
    }
}
