namespace ConnectedOps.Application.Billing;

public interface IPaymentService
{
    Task<IReadOnlyCollection<PaymentTransactionDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto?> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentTransactionDto> RefundPaymentAsync(Guid id, string? reason = null, CancellationToken cancellationToken = default);
}
