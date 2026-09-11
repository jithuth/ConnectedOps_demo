using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Billing;

public sealed class Invoice : BaseEntity
{
    private readonly List<InvoiceItem> _items = [];
    private readonly List<PaymentTransaction> _payments = [];

    private Invoice()
    {
    }

    public Invoice(
        Guid tenantId,
        string invoiceNumber,
        string title,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        string currency = "USD",
        string? billingContactName = null,
        string? billingEmail = null,
        string? billingAddress = null,
        string? notes = null,
        string? termsAndConditions = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("Invoice number is required.", nameof(invoiceNumber));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        TenantId = tenantId;
        InvoiceNumber = invoiceNumber.Trim().ToUpperInvariant();
        Title = title.Trim();
        IssueDateUtc = issueDateUtc;
        DueDateUtc = dueDateUtc;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        BillingContactName = billingContactName?.Trim();
        BillingEmail = billingEmail?.Trim();
        BillingAddress = billingAddress?.Trim();
        Notes = notes?.Trim();
        TermsAndConditions = termsAndConditions?.Trim();
        Status = InvoiceStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; }
    public DateTime IssueDateUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public string Currency { get; private set; } = "USD";

    public decimal SubTotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal BalanceDue => Math.Max(0m, TotalAmount - AmountPaid);

    public string? BillingContactName { get; private set; }
    public string? BillingEmail { get; private set; }
    public string? BillingAddress { get; private set; }
    public string? Notes { get; private set; }
    public string? TermsAndConditions { get; private set; }

    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<PaymentTransaction> Payments => _payments.AsReadOnly();

    public InvoiceItem AddItem(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercentage = 0m)
    {
        var item = new InvoiceItem(
            TenantId,
            Id,
            description,
            quantity,
            unitPrice,
            taxRatePercentage);

        _items.Add(item);
        RecalculateTotals();
        return item;
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(x => x.Id == itemId);
        if (item is not null)
        {
            _items.Remove(item);
            RecalculateTotals();
        }
    }

    public void RecalculateTotals()
    {
        SubTotal = _items.Sum(x => x.SubTotal);
        TaxAmount = _items.Sum(x => x.TaxAmount);
        TotalAmount = _items.Sum(x => x.LineTotal);
        MarkUpdated();
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");

        Status = InvoiceStatus.Issued;
        MarkUpdated();
    }

    public void ApplyPayment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Payment amount must be greater than zero.", nameof(amount));

        AmountPaid += amount;

        if (AmountPaid >= TotalAmount)
        {
            Status = InvoiceStatus.Paid;
            PaidAtUtc = DateTime.UtcNow;
        }
        else
        {
            Status = InvoiceStatus.PartiallyPaid;
        }

        MarkUpdated();
    }

    public void MarkVoid(string? reason = null)
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Paid invoices cannot be voided.");

        Status = InvoiceStatus.Void;
        Notes = string.IsNullOrWhiteSpace(reason) ? Notes : $"{Notes}\n[Voided]: {reason}".Trim();
        MarkUpdated();
    }

    public void MarkOverdue()
    {
        if (Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && DateTime.UtcNow > DueDateUtc)
        {
            Status = InvoiceStatus.Overdue;
            MarkUpdated();
        }
    }

    public void UpdateBillingInfo(
        string? contactName,
        string? email,
        string? address,
        string? notes)
    {
        BillingContactName = contactName?.Trim();
        BillingEmail = email?.Trim();
        BillingAddress = address?.Trim();
        Notes = notes?.Trim();
        MarkUpdated();
    }
}
