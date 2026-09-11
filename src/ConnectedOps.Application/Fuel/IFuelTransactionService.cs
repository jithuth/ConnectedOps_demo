using ConnectedOps.Application.Vehicles;

namespace ConnectedOps.Application.Fuel;

public interface IFuelTransactionService
{
    Task<PagedResult<FuelTransactionListItemDto>> GetTransactionsPagedAsync(
        FuelTransactionQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<FuelTransactionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FuelTransactionDto> CreateAsync(
        CreateFuelTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelTransactionDto> UpdateAsync(
        Guid id,
        UpdateFuelTransactionRequest request,
        CancellationToken cancellationToken = default);

    Task<FuelTransactionDto> CancelAsync(
        Guid id,
        CancelFuelTransactionRequest? request = null,
        CancellationToken cancellationToken = default);

    // Document Attachments
    Task<FuelTransactionDocumentDto> AddDocumentAsync(
        Guid transactionId,
        AddFuelTransactionDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteDocumentAsync(
        Guid transactionId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}
