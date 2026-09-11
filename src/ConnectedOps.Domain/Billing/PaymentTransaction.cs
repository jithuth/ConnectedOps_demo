using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Billing;

public sealed class PaymentTransaction : BaseEntity
{
    private PaymentTransaction()
    {
    }

    public PaymentTransaction(
        Guid tenantId,
        string transactionReference,
        decimal amount,
        string currency,
        PaymentMethod paymentMethod,
        Guid? invoiceId = null,
        string? payerEmail = null,
        string? lastFourDigits = null,
        string? cardBrand = null,
        string? notes = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(transactionReference))
            throw new ArgumentException("Transaction reference is required.", nameof(transactionReference));
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        TenantId = tenantId;
        InvoiceId = invoiceId;
        TransactionReference = transactionReference.Trim().ToUpperInvariant();
        Amount = amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        PaymentMethod = paymentMethod;
        PayerEmail = payerEmail?.Trim();
        LastFourDigits = lastFourDigits?.Trim();
        CardBrand = cardBrand?.Trim();
        Notes = notes?.Trim();
        Status = PaymentStatus.Pending;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public Guid TenantId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Invoice? Invoice { get; private set; }

    public string TransactionReference { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentMethod PaymentMethod { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime ProcessedAtUtc { get; private set; }

    public string? GatewayTransactionId { get; private set; }
    public string? GatewayResponseCode { get; private set; }
    public string? GatewayResponseMessage { get; private set; }

    public string? PayerEmail { get; private set; }
    public string? LastFourDigits { get; private set; }
    public string? CardBrand { get; private set; }
    public string? Notes { get; private set; }

    public void MarkSucceeded(string? gatewayTransactionId = null, string? responseMessage = null)
    {
        Status = PaymentStatus.Succeeded;
        GatewayTransactionId = gatewayTransactionId?.Trim();
        GatewayResponseMessage = responseMessage?.Trim();
        ProcessedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void MarkFailed(string responseCode, string responseMessage)
    {
        Status = PaymentStatus.Failed;
        GatewayResponseCode = responseCode?.Trim();
        GatewayResponseMessage = responseMessage?.Trim();
        ProcessedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Refund(string? reason = null)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Only succeeded payments can be refunded.");

        Status = PaymentStatus.Refunded;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes}\n[Refunded]: {reason}".Trim();
        MarkUpdated();
    }
}
