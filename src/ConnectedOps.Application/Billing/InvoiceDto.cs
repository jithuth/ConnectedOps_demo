using ConnectedOps.Domain.Billing;

namespace ConnectedOps.Application.Billing;

public sealed record InvoiceDto(
    Guid Id,
    Guid TenantId,
    string InvoiceNumber,
    string Title,
    InvoiceStatus Status,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    DateTime? PaidAtUtc,
    string Currency,
    decimal SubTotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string? BillingContactName,
    string? BillingEmail,
    string? BillingAddress,
    string? Notes,
    string? TermsAndConditions,
    IReadOnlyCollection<InvoiceItemDto> Items,
    IReadOnlyCollection<PaymentTransactionDto> Payments,
    DateTime CreatedAtUtc);

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string Title,
    InvoiceStatus Status,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    DateTime? PaidAtUtc,
    string Currency,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    string? BillingContactName,
    string? BillingEmail,
    int ItemCount);

public sealed record InvoiceItemDto(
    Guid Id,
    Guid InvoiceId,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxRatePercentage,
    decimal TaxAmount,
    decimal SubTotal,
    decimal LineTotal);

public sealed record CreateInvoiceRequest
{
    public string Title { get; init; } = string.Empty;
    public DateTime DueDateUtc { get; init; }
    public string Currency { get; init; } = "USD";
    public string? BillingContactName { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Notes { get; init; }
    public string? TermsAndConditions { get; init; }
    public List<CreateInvoiceItemRequest> Items { get; init; } = [];
}

public sealed record CreateInvoiceItemRequest
{
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public decimal TaxRatePercentage { get; init; }
}

public sealed record UpdateInvoiceBillingInfoRequest
{
    public string? BillingContactName { get; init; }
    public string? BillingEmail { get; init; }
    public string? BillingAddress { get; init; }
    public string? Notes { get; init; }
}
