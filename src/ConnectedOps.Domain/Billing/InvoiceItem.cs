using ConnectedOps.Domain.Common;

namespace ConnectedOps.Domain.Billing;

public sealed class InvoiceItem : BaseEntity
{
    private InvoiceItem()
    {
    }

    public InvoiceItem(
        Guid tenantId,
        Guid invoiceId,
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercentage = 0m)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId is required.", nameof(invoiceId));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));
        if (taxRatePercentage < 0)
            throw new ArgumentException("Tax rate cannot be negative.", nameof(taxRatePercentage));

        TenantId = tenantId;
        InvoiceId = invoiceId;
        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRatePercentage = taxRatePercentage;

        Recalculate();
    }

    public Guid TenantId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRatePercentage { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal SubTotal { get; private set; }
    public decimal LineTotal { get; private set; }

    public void Update(
        string description,
        decimal quantity,
        decimal unitPrice,
        decimal taxRatePercentage)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRatePercentage = Math.Max(0m, taxRatePercentage);

        Recalculate();
        MarkUpdated();
    }

    private void Recalculate()
    {
        SubTotal = Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);
        TaxAmount = Math.Round(SubTotal * (TaxRatePercentage / 100m), 2, MidpointRounding.AwayFromZero);
        LineTotal = SubTotal + TaxAmount;
    }
}
