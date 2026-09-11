using ConnectedOps.Application.Accounting;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Accounting;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Accounting;

public sealed class AccountingLedgerService : IAccountingLedgerService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public AccountingLedgerService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<IReadOnlyCollection<GeneralLedgerAccountDto>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await EnsureDefaultAccountsAsync(tenantId, cancellationToken);

        var accounts = await _dbContext.GeneralLedgerAccounts
            .Include(x => x.Entries)
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderBy(x => x.AccountCode)
            .ToListAsync(cancellationToken);

        return accounts.Select(MapToDto).ToList();
    }

    public async Task<GeneralLedgerAccountDto?> GetAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var account = await _dbContext.GeneralLedgerAccounts
            .Include(x => x.Entries)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return account is null ? null : MapToDto(account);
    }

    public async Task<GeneralLedgerAccountDto> CreateAccountAsync(CreateGeneralLedgerAccountRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var code = request.AccountCode.Trim().ToUpperInvariant();

        var exists = await _dbContext.GeneralLedgerAccounts
            .AnyAsync(x => x.TenantId == tenantId && x.AccountCode == code, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"Account code '{code}' already exists.");

        var account = new GeneralLedgerAccount(
            tenantId,
            code,
            request.AccountName,
            request.Category,
            request.Description,
            isSystem: false);

        _dbContext.GeneralLedgerAccounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(account);
    }

    public async Task<GeneralLedgerAccountDto> UpdateAccountAsync(Guid id, UpdateGeneralLedgerAccountRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var account = await _dbContext.GeneralLedgerAccounts
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (account is null)
            throw new KeyNotFoundException($"Account with ID '{id}' was not found.");

        account.UpdateDetails(request.AccountName, request.Description);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(account);
    }

    public async Task<IReadOnlyCollection<LedgerEntryDto>> GetEntriesAsync(Guid? accountId = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var query = _dbContext.LedgerEntries
            .Include(x => x.Account)
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId);

        if (accountId.HasValue)
        {
            query = query.Where(x => x.AccountId == accountId.Value);
        }

        var entries = await query
            .OrderByDescending(x => x.EntryDateUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return entries.Select(MapEntryToDto).ToList();
    }

    public async Task<IReadOnlyCollection<LedgerEntryDto>> RecordJournalEntryAsync(CreateJournalEntryRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Journal entry must contain at least one debit and credit line.");
        }

        var totalDebits = request.Lines.Sum(l => l.DebitAmount);
        var totalCredits = request.Lines.Sum(l => l.CreditAmount);

        if (totalDebits != totalCredits)
        {
            throw new InvalidOperationException($"Double-entry imbalance: Total debits (${totalDebits:N2}) must equal total credits (${totalCredits:N2}).");
        }

        var year = DateTime.UtcNow.Year;
        var count = await _dbContext.LedgerEntries
            .Where(x => x.TenantId == tenantId && x.EntryDateUtc.Year == year)
            .Select(x => x.EntryNumber)
            .Distinct()
            .CountAsync(cancellationToken);

        var entryNumber = $"JE-{year}-{(count + 1):D4}";

        var entries = new List<LedgerEntry>();

        foreach (var line in request.Lines)
        {
            var entry = new LedgerEntry(
                tenantId,
                line.AccountId,
                entryNumber,
                request.EntryDateUtc,
                line.DebitAmount,
                line.CreditAmount,
                line.Description ?? request.Description,
                request.ReferenceType,
                request.ReferenceId,
                request.Currency);

            entries.Add(entry);
        }

        _dbContext.LedgerEntries.AddRange(entries);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetEntriesAsync(null, cancellationToken);
    }

    public async Task<FinancialSummaryDto> GetFinancialSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        await EnsureDefaultAccountsAsync(tenantId, cancellationToken);

        var accounts = await _dbContext.GeneralLedgerAccounts
            .Include(x => x.Entries)
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var revenueAccounts = accounts
            .Where(x => x.Category == AccountCategory.Revenue)
            .Select(x => new AccountBalanceDto(x.AccountCode, x.AccountName, CalculateBalance(x)))
            .ToList();

        var expenseAccounts = accounts
            .Where(x => x.Category == AccountCategory.Expense)
            .Select(x => new AccountBalanceDto(x.AccountCode, x.AccountName, CalculateBalance(x)))
            .ToList();

        var totalRevenue = revenueAccounts.Sum(x => x.Balance);
        var totalExpenses = expenseAccounts.Sum(x => x.Balance);
        var netIncome = totalRevenue - totalExpenses;

        var arAccount = accounts.FirstOrDefault(x => x.AccountCode == "1200");
        var totalAR = arAccount != null ? CalculateBalance(arAccount) : 0m;

        var cashAccount = accounts.FirstOrDefault(x => x.AccountCode == "1000");
        var totalCash = cashAccount != null ? CalculateBalance(cashAccount) : 0m;

        var recentEntries = await _dbContext.LedgerEntries
            .Include(x => x.Account)
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.EntryDateUtc)
            .Take(10)
            .Select(x => MapEntryToDto(x))
            .ToListAsync(cancellationToken);

        return new FinancialSummaryDto(
            totalRevenue,
            totalAR,
            totalCash,
            totalExpenses,
            netIncome,
            revenueAccounts,
            expenseAccounts,
            recentEntries);
    }

    public async Task EnsureDefaultAccountsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existingCodes = await _dbContext.GeneralLedgerAccounts
            .Where(x => x.TenantId == tenantId)
            .Select(x => x.AccountCode)
            .ToListAsync(cancellationToken);

        var defaults = new List<GeneralLedgerAccount>
        {
            new(tenantId, "1000", "Cash & Operating Bank Account", AccountCategory.Asset, "Primary operational liquid funds", isSystem: true),
            new(tenantId, "1200", "Accounts Receivable", AccountCategory.Asset, "Outstanding customer invoices & receivables", isSystem: true),
            new(tenantId, "2000", "Accounts Payable & Liabilities", AccountCategory.Liability, "Vendor obligations and short-term debt", isSystem: true),
            new(tenantId, "3000", "Retained Earnings & Owner Equity", AccountCategory.Equity, "Accumulated enterprise equity", isSystem: true),
            new(tenantId, "4000", "SaaS Subscription Revenue", AccountCategory.Revenue, "Fleet intelligence subscription revenue", isSystem: true),
            new(tenantId, "4100", "Asset License & Platform Add-on Revenue", AccountCategory.Revenue, "Add-on telemetry and asset licenses", isSystem: true),
            new(tenantId, "5000", "Fleet Operations & Maintenance Expense", AccountCategory.Expense, "Core fleet operations and upkeep", isSystem: true),
            new(tenantId, "5100", "IoT Telematics & Cloud Data Expense", AccountCategory.Expense, "Hardware telemetry, cellular SIMs, and sensors", isSystem: true),
            new(tenantId, "5200", "General & Administrative Expense", AccountCategory.Expense, "Software licenses, legal, and operational overhead", isSystem: true)
        };

        var toAdd = defaults.Where(d => !existingCodes.Contains(d.AccountCode)).ToList();
        if (toAdd.Count > 0)
        {
            _dbContext.GeneralLedgerAccounts.AddRange(toAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static decimal CalculateBalance(GeneralLedgerAccount account)
    {
        var debits = account.Entries.Sum(x => x.DebitAmount);
        var credits = account.Entries.Sum(x => x.CreditAmount);

        return account.Category switch
        {
            AccountCategory.Asset or AccountCategory.Expense => debits - credits,
            _ => credits - debits
        };
    }

    private static GeneralLedgerAccountDto MapToDto(GeneralLedgerAccount a) =>
        new(
            a.Id,
            a.TenantId,
            a.AccountCode,
            a.AccountName,
            a.Category,
            a.Description,
            a.IsActive,
            a.IsSystem,
            CalculateBalance(a),
            a.CreatedAtUtc);

    private static LedgerEntryDto MapEntryToDto(LedgerEntry e) =>
        new(
            e.Id,
            e.TenantId,
            e.AccountId,
            e.Account?.AccountCode ?? "",
            e.Account?.AccountName ?? "",
            e.EntryNumber,
            e.EntryDateUtc,
            e.DebitAmount,
            e.CreditAmount,
            e.Description,
            e.ReferenceType,
            e.ReferenceId,
            e.Currency,
            e.CreatedAtUtc);
}
