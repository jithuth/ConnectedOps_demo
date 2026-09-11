using ConnectedOps.Application.Billing;
using ConnectedOps.Application.Common.Interfaces;
using ConnectedOps.Domain.Billing;
using ConnectedOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ConnectedOps.Infrastructure.Billing;

public sealed class BillingDashboardService : IBillingDashboardService
{
    private readonly ConnectedOpsDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly ITenantSubscriptionService _subscriptionService;

    public BillingDashboardService(
        ConnectedOpsDbContext dbContext,
        ICurrentUserContext currentUserContext,
        ITenantSubscriptionService subscriptionService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _subscriptionService = subscriptionService;
    }

    public async Task<BillingDashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = GetCurrentTenantId();
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        var currentSubscription = await _subscriptionService.GetCurrentSubscriptionAsync(cancellationToken);

        var activeInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status != InvoiceStatus.Void)
            .ToListAsync(cancellationToken);

        var totalOutstandingReceivables = activeInvoices
            .Where(x => x.Status != InvoiceStatus.Paid)
            .Sum(x => x.BalanceDue);

        var pendingInvoicesCount = activeInvoices
            .Count(x => x.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid);

        var overdueInvoicesCount = activeInvoices
            .Count(x => x.Status == InvoiceStatus.Overdue || (x.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && x.DueDateUtc < now));

        var totalPaidLast30Days = await _dbContext.PaymentTransactions
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.Status == PaymentStatus.Succeeded && x.ProcessedAtUtc >= thirtyDaysAgo)
            .SumAsync(x => x.Amount, cancellationToken);

        var recentInvoices = await _dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.Items)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.IssueDateUtc)
            .Take(5)
            .Select(x => new InvoiceListItemDto(
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
                x.Items.Count))
            .ToListAsync(cancellationToken);

        var recentPayments = await _dbContext.PaymentTransactions
            .AsNoTracking()
            .Include(x => x.Invoice)
            .Where(x => x.TenantId == tenantId)
            .OrderByDescending(x => x.ProcessedAtUtc)
            .Take(5)
            .Select(p => new PaymentTransactionDto(
                p.Id,
                p.TenantId,
                p.InvoiceId,
                p.Invoice != null ? p.Invoice.InvoiceNumber : null,
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
                p.Notes))
            .ToListAsync(cancellationToken);

        return new BillingDashboardSummaryDto(
            currentSubscription,
            totalOutstandingReceivables,
            totalPaidLast30Days,
            pendingInvoicesCount,
            overdueInvoicesCount,
            recentInvoices,
            recentPayments);
    }

    private Guid GetCurrentTenantId()
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (_currentUserContext.TenantId is not Guid tenantId)
            throw new UnauthorizedAccessException("Tenant ID is missing.");

        return tenantId;
    }
}
