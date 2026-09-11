namespace ConnectedOps.Application.Billing;

public interface IInvoiceService
{
    Task<IReadOnlyCollection<InvoiceListItemDto>> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request, CancellationToken cancellationToken = default);
    Task<InvoiceDto> IssueInvoiceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> VoidInvoiceAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default);
    Task UpdateBillingInfoAsync(Guid id, UpdateInvoiceBillingInfoRequest request, CancellationToken cancellationToken = default);
}
