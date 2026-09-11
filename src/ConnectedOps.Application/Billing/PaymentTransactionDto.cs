using ConnectedOps.Domain.Billing;

namespace ConnectedOps.Application.Billing;

public sealed record PaymentTransactionDto(
    Guid Id,
    Guid TenantId,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string TransactionReference,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    PaymentStatus Status,
    DateTime ProcessedAtUtc,
    string? GatewayTransactionId,
    string? GatewayResponseMessage,
    string? PayerEmail,
    string? LastFourDigits,
    string? CardBrand,
    string? Notes);

public sealed record RecordPaymentRequest
{
    public Guid? InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public PaymentMethod PaymentMethod { get; init; } = PaymentMethod.CreditCard;
    public string? PayerEmail { get; init; }
    public string? LastFourDigits { get; init; }
    public string? CardBrand { get; init; }
    public string? Notes { get; init; }
    public string? GatewayTransactionId { get; init; }
}

public sealed record BillingDashboardSummaryDto(
    TenantSubscriptionDto? CurrentSubscription,
    decimal TotalOutstandingReceivables,
    decimal TotalPaidLast30Days,
    int PendingInvoicesCount,
    int OverdueInvoicesCount,
    IReadOnlyCollection<InvoiceListItemDto> RecentInvoices,
    IReadOnlyCollection<PaymentTransactionDto> RecentPayments);
