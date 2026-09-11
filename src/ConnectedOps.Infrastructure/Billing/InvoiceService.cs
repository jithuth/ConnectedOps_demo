using ConnectedOps.Application.Accounting;
using ConnectedOps.Application.Billing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Notifications;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Billing;

public sealed class InvoiceService : IInvoiceService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAccountingLedgerService _accountingService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAccountingLedgerService accountingService,
        IEmailNotificationService emailService,
        ILogger<InvoiceService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _accountingService = accountingService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<InvoiceListItemDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        // Check for overdue invoices and update statuses in memory
        var now = DateTime.UtcNow;
        var invoices = await _dbContext.Invoices
            .Include(x => x.Items)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.IssueDateUtc)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var inv in invoices)
        {
            if (inv.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && now > inv.DueDateUtc)
            {
                inv.MarkOverdue();
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return invoices.Select(x => new InvoiceListItemDto(
            x.Id,
            x.InvoiceNumber,
            x.Title,
            x.Status,
            x.IssueDateUtc,
            x.DueDateUtc,
            x.PaidAtUtc,
            x.Currency,
            x.TotalAmount,
            x.AmountPaid,
            x.BalanceDue,
            x.BillingContactName,
            x.BillingEmail,
            x.Items.Count)).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var invoice = await _dbContext.Invoices
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (invoice is null)
            return null;

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var year = DateTime.UtcNow.Year;
        var countThisYear = await _dbContext.Invoices
            .CountAsync(x => x.TenantId == tenantId && x.IssueDateUtc.Year == year, cancellationToken);

        var invoiceNumber = $"INV-{year}-{(countThisYear + 1):D4}";

        var invoice = new Invoice(
            tenantId,
            invoiceNumber,
            request.Title,
            DateTime.UtcNow,
            request.DueDateUtc > DateTime.UtcNow ? request.DueDateUtc : DateTime.UtcNow.AddDays(30),
            request.Currency,
            request.BillingContactName,
            request.BillingEmail,
            request.BillingAddress,
            request.Notes,
            request.TermsAndConditions);

        foreach (var item in request.Items)
        {
            invoice.AddItem(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.TaxRatePercentage);
        }

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> IssueInvoiceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var invoice = await _dbContext.Invoices
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (invoice is null)
            throw new KeyNotFoundException($"Invoice with ID '{id}' was not found.");

        invoice.Issue();
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Auto post to General Ledger (Debit: 1200 Accounts Receivable, Credit: 4000 Subscription Revenue)
        try
        {
            await _accountingService.EnsureDefaultAccountsAsync(tenantId, cancellationToken);
            var accounts = await _accountingService.GetAccountsAsync(cancellationToken);
            var arAccount = accounts.FirstOrDefault(x => x.AccountCode == "1200");
            var revAccount = accounts.FirstOrDefault(x => x.AccountCode == "4000");

            if (arAccount is not null && revAccount is not null)
            {
                await _accountingService.RecordJournalEntryAsync(new CreateJournalEntryRequest
                {
                    Description = $"Invoice {invoice.InvoiceNumber} issued to {invoice.BillingContactName ?? "Customer"}",
                    ReferenceType = "Invoice",
                    ReferenceId = invoice.Id,
                    Currency = invoice.Currency,
                    Lines =
                    [
                        new() { AccountId = arAccount.Id, DebitAmount = invoice.TotalAmount, CreditAmount = 0 },
                        new() { AccountId = revAccount.Id, DebitAmount = 0, CreditAmount = invoice.TotalAmount }
                    ]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create automated journal entry for issued invoice {InvoiceNumber}", invoice.InvoiceNumber);
        }

        // Send Email Notification
        try
        {
            await _emailService.SendInvoiceReadyEmailAsync(invoice.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send invoice ready email for invoice {InvoiceNumber}", invoice.InvoiceNumber);
        }

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> VoidInvoiceAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var invoice = await _dbContext.Invoices
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (invoice is null)
            throw new KeyNotFoundException($"Invoice with ID '{id}' was not found.");

        invoice.MarkVoid(reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToDto(invoice);
    }

    public async Task UpdateBillingInfoAsync(Guid id, UpdateInvoiceBillingInfoRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (invoice is null)
            throw new KeyNotFoundException($"Invoice with ID '{id}' was not found.");

        invoice.UpdateBillingInfo(
            request.BillingContactName,
            request.BillingEmail,
            request.BillingAddress,
            request.Notes);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static InvoiceDto MapToDto(Invoice x) =>
        new(
            x.Id,
            x.TenantId,
            x.InvoiceNumber,
            x.Title,
            x.Status,
            x.IssueDateUtc,
            x.DueDateUtc,
            x.PaidAtUtc,
            x.Currency,
            x.SubTotal,
            x.TaxAmount,
            x.TotalAmount,
            x.AmountPaid,
            x.BalanceDue,
            x.BillingContactName,
            x.BillingEmail,
            x.BillingAddress,
            x.Notes,
            x.TermsAndConditions,
            x.Items.Select(i => new InvoiceItemDto(
                i.Id,
                i.InvoiceId,
                i.Description,
                i.Quantity,
                i.UnitPrice,
                i.TaxRatePercentage,
                i.TaxAmount,
                i.SubTotal,
                i.LineTotal)).ToList(),
            x.Payments.Select(p => new PaymentTransactionDto(
                p.Id,
                p.TenantId,
                p.InvoiceId,
                x.InvoiceNumber,
                p.TransactionReference,
                p.Amount,
                p.Currency,
                p.PaymentMethod,
                p.Status,
                p.ProcessedAtUtc,
                p.GatewayTransactionId,
                p.GatewayResponseMessage,
                p.PayerEmail,
                p.LastFourDigits,
                p.CardBrand,
                p.Notes)).ToList(),
            x.CreatedAtUtc);
}
