using ConnectedOps.Application.Accounting;
using ConnectedOps.Application.Billing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Application.Notifications;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConnectedOps.Infrastructure.Billing;

public sealed class PaymentService : IPaymentService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAccountingLedgerService _accountingService;
    private readonly IEmailNotificationService _emailService;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IAccountingLedgerService accountingService,
        IEmailNotificationService emailService,
        ILogger<PaymentService> logger)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _accountingService = accountingService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<PaymentTransactionDto>> GetPaymentsAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var payments = await _dbContext.PaymentTransactions
            .Include(x => x.Invoice)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ProcessedAtUtc)
            .ToListAsync(cancellationToken);

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentTransactionDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var payment = await _dbContext.PaymentTransactions
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        return payment is null ? null : MapToDto(payment);
    }

    public async Task<PaymentTransactionDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomSuffix = Random.Shared.Next(100000, 999999);
        var txnRef = $"TXN-{dateStr}-{randomSuffix}";

        Invoice? invoice = null;
        if (request.InvoiceId.HasValue)
        {
            invoice = await _dbContext.Invoices
                .FirstOrDefaultAsync(x => x.Id == request.InvoiceId.Value && x.TenantId == tenantId, cancellationToken);

            if (invoice is null)
                throw new KeyNotFoundException($"Invoice with ID '{request.InvoiceId.Value}' was not found.");
        }

        var payment = new PaymentTransaction(
            tenantId,
            txnRef,
            request.Amount,
            request.Currency,
            request.PaymentMethod,
            request.InvoiceId,
            request.PayerEmail ?? invoice?.BillingEmail,
            request.LastFourDigits,
            request.CardBrand,
            request.Notes);

        payment.MarkSucceeded(request.GatewayTransactionId, "Payment processed successfully.");

        if (invoice is not null)
        {
            invoice.ApplyPayment(request.Amount);
        }

        _dbContext.PaymentTransactions.Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Auto post to General Ledger (Debit: 1000 Cash / Bank, Credit: 1200 Accounts Receivable)
        try
        {
            await _accountingService.EnsureDefaultAccountsAsync(tenantId, cancellationToken);
            var accounts = await _accountingService.GetAccountsAsync(cancellationToken);
            var cashAccount = accounts.FirstOrDefault(x => x.AccountCode == "1000");
            var arAccount = accounts.FirstOrDefault(x => x.AccountCode == "1200");

            if (cashAccount is not null && arAccount is not null)
            {
                await _accountingService.RecordJournalEntryAsync(new CreateJournalEntryRequest
                {
                    Description = $"Payment received {payment.TransactionReference}" + (invoice != null ? $" for Invoice {invoice.InvoiceNumber}" : ""),
                    ReferenceType = "Payment",
                    ReferenceId = payment.Id,
                    Currency = payment.Currency,
                    Lines =
                    [
                        new() { AccountId = cashAccount.Id, DebitAmount = payment.Amount, CreditAmount = 0 },
                        new() { AccountId = arAccount.Id, DebitAmount = 0, CreditAmount = payment.Amount }
                    ]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create automated journal entry for payment {TransactionReference}", payment.TransactionReference);
        }

        // Send Email Receipt
        try
        {
            await _emailService.SendPaymentReceiptEmailAsync(payment.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send receipt email for payment {TransactionReference}", payment.TransactionReference);
        }

        return MapToDto(payment);
    }

    public async Task<PaymentTransactionDto> RefundPaymentAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();

        var payment = await _dbContext.PaymentTransactions
            .Include(x => x.Invoice)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, cancellationToken);

        if (payment is null)
            throw new KeyNotFoundException($"Payment with ID '{id}' was not found.");

        payment.Refund(reason);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Auto post to General Ledger reverse entry (Debit: 4000 Subscription Revenue, Credit: 1000 Cash / Bank)
        try
        {
            await _accountingService.EnsureDefaultAccountsAsync(tenantId, cancellationToken);
            var accounts = await _accountingService.GetAccountsAsync(cancellationToken);
            var cashAccount = accounts.FirstOrDefault(x => x.AccountCode == "1000");
            var revAccount = accounts.FirstOrDefault(x => x.AccountCode == "4000");

            if (cashAccount is not null && revAccount is not null)
            {
                await _accountingService.RecordJournalEntryAsync(new CreateJournalEntryRequest
                {
                    Description = $"Refund processed for payment {payment.TransactionReference}: {reason ?? "Customer refund"}",
                    ReferenceType = "Refund",
                    ReferenceId = payment.Id,
                    Currency = payment.Currency,
                    Lines =
                    [
                        new() { AccountId = revAccount.Id, DebitAmount = payment.Amount, CreditAmount = 0 },
                        new() { AccountId = cashAccount.Id, DebitAmount = 0, CreditAmount = payment.Amount }
                    ]
                }, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create refund journal entry for payment {TransactionReference}", payment.TransactionReference);
        }

        return MapToDto(payment);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }

    private static PaymentTransactionDto MapToDto(PaymentTransaction p) =>
        new(
            p.Id,
            p.TenantId,
            p.InvoiceId,
            p.Invoice?.InvoiceNumber,
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
            p.Notes);
}
